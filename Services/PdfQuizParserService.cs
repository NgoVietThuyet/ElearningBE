using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using ElearningAPI.Dtos;

namespace ElearningAPI.Services
{
    public interface IPdfQuizParserService
    {
        ParsedQuizDto ParseQuizFromPdf(Stream pdfStream);
    }

    public class PdfQuizParserService : IPdfQuizParserService
    {
        public ParsedQuizDto ParseQuizFromPdf(Stream pdfStream)
        {
            var quiz = new ParsedQuizDto();
            var fullTextBuilder = new StringBuilder();

            try
            {
                using (var document = PdfDocument.Open(pdfStream))
                {
                    foreach (var page in document.GetPages())
                    {
                        fullTextBuilder.AppendLine(page.Text);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading PDF: {ex.Message}");
                throw new InvalidOperationException("Không thể đọc tệp PDF. Tệp có thể bị hỏng hoặc bảo mật.", ex);
            }

            string fullText = fullTextBuilder.ToString();
            
            // Normalize spaces and line endings
            fullText = fullText.Replace("\r\n", "\n").Replace("\r", "\n");

            // Normalize Unicode to FormC (Precomposed) to resolve Vietnamese decomposed character matching issues in PDFs
            fullText = fullText.Normalize(NormalizationForm.FormC);

            // 1. Try to find a global answer key table at the end of the document
            var globalAnswers = ParseGlobalAnswerKey(fullText);

            // 2. Identify all question block boundaries using the updated robust pattern
            var questionRegex = new Regex(@"(?:Câu|Question)\s*(\d+)\s*[\.:-]", RegexOptions.IgnoreCase);
            var matches = questionRegex.Matches(fullText);

            var questionBlocks = new List<(int QuestionNumber, string Content)>();

            for (int i = 0; i < matches.Count; i++)
            {
                int startIdx = matches[i].Index;
                int endIdx = (i < matches.Count - 1) ? matches[i + 1].Index : fullText.Length;
                
                string blockContent = fullText.Substring(startIdx, endIdx - startIdx).Trim();
                if (int.TryParse(matches[i].Groups[1].Value, out int qNum))
                {
                    questionBlocks.Add((qNum, blockContent));
                }
            }

            // If no blocks were found using the typical format, let's try a fallback splitting on digits like "1." or "1/"
            if (questionBlocks.Count == 0)
            {
                var fallbackRegex = new Regex(@"(\d+)[\.\)/-]\s+", RegexOptions.IgnoreCase);
                var fallbackMatches = fallbackRegex.Matches(fullText);
                for (int i = 0; i < fallbackMatches.Count; i++)
                {
                    int startIdx = fallbackMatches[i].Index;
                    int endIdx = (i < fallbackMatches.Count - 1) ? fallbackMatches[i + 1].Index : fullText.Length;
                    string blockContent = fullText.Substring(startIdx, endIdx - startIdx).Trim();
                    if (int.TryParse(fallbackMatches[i].Groups[1].Value, out int qNum))
                    {
                        questionBlocks.Add((qNum, blockContent));
                    }
                }
            }

            // 3. Parse each question block
            var parsedQuestions = new List<ParsedQuestionDto>();
            var optionRegex = new Regex(@"(A|B|C|D)\s*[\.\:-]\s*(.*?)(?=[A-D]\s*[\.\:-]|(?:Đáp án|Chọn|Answer|Key)|\n|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var answerRegex = new Regex(@"(?:Đáp án|Chọn|Đáp án đúng|Đáp án là|Answer|Key)\s*(?:đúng|chính xác|là)?\s*[:\.-]?\s*([A-D])\b", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            foreach (var block in questionBlocks)
            {
                string blockText = block.Content;

                // Find options in this block
                var optionMatches = optionRegex.Matches(blockText);
                var options = new List<string>();
                var optionMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (Match m in optionMatches)
                {
                    string label = m.Groups[1].Value.Trim().ToUpper();
                    string text = m.Groups[2].Value.Trim();
                    // Strip trailing symbols or clean up option text
                    text = Regex.Replace(text, @"\s+", " ");
                    optionMap[label] = text;
                }

                // Standardize options order to A, B, C, D
                string optA = optionMap.TryGetValue("A", out var valA) ? valA : string.Empty;
                string optB = optionMap.TryGetValue("B", out var valB) ? valB : string.Empty;
                string optC = optionMap.TryGetValue("C", out var valC) ? valC : string.Empty;
                string optD = optionMap.TryGetValue("D", out var valD) ? valD : string.Empty;

                // Only count as a valid question if we have at least 2 options
                int filledOptionsCount = 0;
                if (!string.IsNullOrEmpty(optA)) filledOptionsCount++;
                if (!string.IsNullOrEmpty(optB)) filledOptionsCount++;
                if (!string.IsNullOrEmpty(optC)) filledOptionsCount++;
                if (!string.IsNullOrEmpty(optD)) filledOptionsCount++;

                if (filledOptionsCount < 2)
                {
                    // Maybe the block is just some text or headers, skip it
                    continue;
                }

                options.Add(string.IsNullOrEmpty(optA) ? "Đáp án A" : optA);
                options.Add(string.IsNullOrEmpty(optB) ? "Đáp án B" : optB);
                options.Add(string.IsNullOrEmpty(optC) ? "Đáp án C" : optC);
                options.Add(string.IsNullOrEmpty(optD) ? "Đáp án D" : optD);

                // Extract Question Text: everything before the first option match
                int firstOptionIndex = blockText.Length;
                foreach (Match m in optionMatches)
                {
                    if (m.Index < firstOptionIndex)
                    {
                        firstOptionIndex = m.Index;
                    }
                }

                string questionTitle = blockText.Substring(0, firstOptionIndex).Trim();
                // Strip the question header prefix (e.g. "Câu 1:")
                var headerMatch = Regex.Match(questionTitle, @"^(?:Câu|Question|\d+)\s*\d*\s*[\.\):-]\s*", RegexOptions.IgnoreCase);
                if (headerMatch.Success)
                {
                    questionTitle = questionTitle.Substring(headerMatch.Length).Trim();
                }
                
                // Clean extra whitespaces
                questionTitle = Regex.Replace(questionTitle, @"\s+", " ");

                // Determine correct answer
                int correctIdx = 0;
                bool foundAnswer = false;

                // 1. Look for inline answer inside the block
                var inlineMatch = answerRegex.Match(blockText);
                if (inlineMatch.Success)
                {
                    string answerLetter = inlineMatch.Groups[1].Value.ToUpper();
                    correctIdx = answerLetter[0] - 'A';
                    foundAnswer = true;
                }

                // 2. Fallback to global answer key table
                if (!foundAnswer && globalAnswers.TryGetValue(block.QuestionNumber, out int globalCorrectIdx))
                {
                    correctIdx = globalCorrectIdx;
                    foundAnswer = true;
                }

                // 3. Fallback to default (0 - A)
                if (!foundAnswer)
                {
                    correctIdx = 0; 
                }

                parsedQuestions.Add(new ParsedQuestionDto
                {
                    Question = questionTitle,
                    Options = options,
                    Answer = correctIdx,
                    CorrectIndex = correctIdx
                });
            }

            quiz.Questions = parsedQuestions;
            quiz.DurationMinutes = parsedQuestions.Count <= 10 ? 15 : (parsedQuestions.Count <= 20 ? 30 : 45); // Set reasonable defaults
            return quiz;
        }

        private Dictionary<int, int> ParseGlobalAnswerKey(string text)
        {
            var answers = new Dictionary<int, int>();
            
            // Search for tables or patterns like:
            // "BẢNG ĐÁP ÁN", "ĐÁP ÁN BÀI THI", "KEY"
            // Example structure: 1.A  2.B  3.C  4.D  5.A
            // or 1-A, 2-B, 3-C, 4-D
            // Let's search in the last 20% of the document or throughout
            // Look for patterns: (\d+)\s*[\.-]\s*([A-D])
            var regex = new Regex(@"(\d+)\s*[\.-]\s*([A-D])(?:\s|$)", RegexOptions.IgnoreCase);
            var matches = regex.Matches(text);

            foreach (Match m in matches)
            {
                if (int.TryParse(m.Groups[1].Value, out int qNum))
                {
                    string letter = m.Groups[2].Value.ToUpper();
                    int idx = letter[0] - 'A';
                    answers[qNum] = idx;
                }
            }

            // Look for table style "1A 2B 3C 4D"
            if (answers.Count == 0)
            {
                var tableRegex = new Regex(@"\b(\d+)([A-D])\b", RegexOptions.IgnoreCase);
                var tableMatches = tableRegex.Matches(text);
                foreach (Match m in tableMatches)
                {
                    if (int.TryParse(m.Groups[1].Value, out int qNum))
                    {
                        string letter = m.Groups[2].Value.ToUpper();
                        int idx = letter[0] - 'A';
                        answers[qNum] = idx;
                    }
                }
            }

            return answers;
        }
    }
}
