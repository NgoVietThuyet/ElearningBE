using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace ElearningAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatbotController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private static readonly HttpClient _httpClient = new HttpClient();

        private const string SystemPrompt = 
            "Bạn là \"GenZBio AI\" — Trợ lý học tập chuyên sâu môn Sinh học lớp 12 tại hệ thống giáo dục EduSmart Việt Nam. Bạn có kiến thức cực kỳ uyên bác về DNA, các cơ chế di truyền phân tử (Nhân đôi DNA, Phiên mã, Dịch mã, Điều hòa hoạt động gen, Đột biến gen...) và toàn bộ chương trình Sinh học 12.\n\n" +
            "Quy tắc bảo mật nghiêm ngặt (Guardrails):\n" +
            "1. Chỉ giải đáp các câu hỏi trong phạm vi môn Sinh học lớp 12 và kiến thức sinh học liên quan.\n" +
            "2. Nếu câu hỏi không liên quan đến Sinh học (Ví dụ: hỏi toán, lý, hóa, lập trình, văn học, lịch sử, địa lý, viết code hoặc chat phiếm không liên quan), bạn phải từ chối lịch sự bằng tiếng Việt: \"Chào bạn, mình là trợ lý AI chuyên biệt về Sinh học 12 của EduSmart. Mình chỉ có thể giúp bạn giải đáp các kiến thức về DNA, di truyền, tiến hóa hoặc sinh thái thôi nhé! Hãy đặt câu hỏi sinh học cho mình nào.\"\n" +
            "3. Hướng dẫn giải bài tập di truyền từng bước (Step-by-step) khoa học, giải thích rõ ràng lý do của mỗi bước tính toán.\n" +
            "4. Sử dụng LaTeX để viết công thức sinh học (ví dụ: bọc kiểu gen trong $Aa$, liên kết gen $\\frac{AB}{ab}$, tần số hoán vị $f$).\n" +
            "5. Giữ thái độ giảng dạy thân thiện, khích lệ học sinh tự học (ngôn từ năng động của thế hệ Gen Z nhưng vẫn đảm bảo tính sư phạm nghiêm túc).";

        public ChatbotController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { message = "Tin nhắn không được để trống." });
            }

            // 1. Lấy API Key từ config
            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                // Fallback nếu người dùng chưa cài đặt API Key, trả về thông báo lỗi thân thiện để tránh crash app
                return Ok(new { 
                    reply = "🔒 **Hệ thống chưa cấu hình Gemini API Key.**\n\nVui lòng cấu hình khóa API trong file `appsettings.json` của Backend:\n```json\n\"Gemini\": {\n  \"ApiKey\": \"KEY_CUA_BAN\"\n}\n```\nđể bắt đầu trò chuyện cùng GenZBio AI nhé!",
                    role = "model"
                });
            }

            try
            {
                // 2. Tạo danh sách contents chuẩn định dạng Gemini API
                var contents = new List<GeminiContent>();

                // Thêm lịch sử hội thoại trước đó (nếu có)
                if (request.History != null && request.History.Count > 0)
                {
                    foreach (var msg in request.History)
                    {
                        var parts = new List<GeminiPart> { new GeminiPart { text = msg.Text } };
                        contents.Add(new GeminiContent
                        {
                            role = msg.Role == "user" ? "user" : "model",
                            parts = parts
                        });
                    }
                }

                // Thêm tin nhắn hiện tại của user kèm ảnh base64 nếu có
                var currentParts = new List<GeminiPart>();
                if (!string.IsNullOrEmpty(request.ImageBase64) && !string.IsNullOrEmpty(request.ImageMimeType))
                {
                    // Loại bỏ tiền tố base64 data:image/...;base64, nếu có
                    var base64Data = request.ImageBase64;
                    if (base64Data.Contains(","))
                    {
                        base64Data = base64Data.Split(',')[1];
                    }

                    currentParts.Add(new GeminiPart
                    {
                        inlineData = new GeminiInlineData
                        {
                            mimeType = request.ImageMimeType,
                            data = base64Data
                        }
                    });
                }
                currentParts.Add(new GeminiPart { text = request.Message });

                contents.Add(new GeminiContent
                {
                    role = "user",
                    parts = currentParts
                });

                // 3. Xây dựng payload request gửi tới Gemini
                var payload = new
                {
                    contents = contents,
                    systemInstruction = new
                    {
                        parts = new[]
                        {
                            new { text = SystemPrompt }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 2048
                    }
                };

                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";
                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

                // 4. Gọi API
                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, new { 
                        message = "Lỗi khi kết nối tới dịch vụ Gemini AI.", 
                        details = responseContent 
                    });
                }

                // 5. Parse phản hồi từ Gemini
                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;
                
                string replyText = "";
                if (root.TryGetProperty("candidates", out var candidates) && 
                    candidates.GetArrayLength() > 0 &&
                    candidates[0].TryGetProperty("content", out var candidateContent) &&
                    candidateContent.TryGetProperty("parts", out var partsElement) &&
                    partsElement.GetArrayLength() > 0)
                {
                    replyText = partsElement[0].GetProperty("text").GetString() ?? "";
                }

                return Ok(new
                {
                    reply = replyText,
                    role = "model"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Lỗi hệ thống khi xử lý yêu cầu chatbot.", 
                    details = ex.Message 
                });
            }
        }
    }

    // ==========================================
    // DTO CLASSES
    // ==========================================
    public class ChatRequest
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = "";

        [JsonPropertyName("history")]
        public List<ChatMessageDto> History { get; set; } = new();

        [JsonPropertyName("imageBase64")]
        public string? ImageBase64 { get; set; }

        [JsonPropertyName("imageMimeType")]
        public string? ImageMimeType { get; set; }
    }

    public class ChatMessageDto
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = ""; // "user" hoặc "model"

        [JsonPropertyName("text")]
        public string Text { get; set; } = "";
    }

    public class GeminiContent
    {
        [JsonPropertyName("role")]
        public string role { get; set; } = "";

        [JsonPropertyName("parts")]
        public List<GeminiPart> parts { get; set; } = new();
    }

    public class GeminiPart
    {
        [JsonPropertyName("text")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? text { get; set; }

        [JsonPropertyName("inlineData")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GeminiInlineData? inlineData { get; set; }
    }

    public class GeminiInlineData
    {
        [JsonPropertyName("mimeType")]
        public string mimeType { get; set; } = "";

        [JsonPropertyName("data")]
        public string data { get; set; } = ""; // Base64
    }
}
