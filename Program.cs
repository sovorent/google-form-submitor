using GoogleFormsToolkitLibrary.Models;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

internal class Program
{
    private static async Task Main(string[] args)
    {

        var googleFormLink = "https://docs.google.com/forms/u/0/d/e/1FAIpQLSexhJuAyR-My0nyD3_MC8p9lDPe40K5SZcjc1BoGv8C4AVbdA";

        var googleTest = "https://docs.google.com/forms/d/15RKb_QdT6TyWHX6ltmOXIGwOLjHUqJYpgc0zj60aL_E";
        var urlGoogleForm = googleFormLink;
        //     Dictionary<string, string> formData = new Dictionary<string, string>()
        //     {

        // {"entry.1799182697", "ชาย"},
        // {"entry.464322521", "ต่ำกว่า 30 ปี"},
        // {"entry.275178132", "5 - 10 ปี"},
        // {"entry.1362310829", "มากกว่า 40,000 บาท"},
        // {"entry.1258678805", "เภสัชกร"},
        // {"entry.630962465", "มากที่สุด"},
        // {"entry.1224097009", "มาก"},
        // {"entry.913155672", "มาก"},
        // {"entry.320952843", "มากที่สุด"},
        // {"entry.1159753960", "มากที่สุด"},
        // {"entry.1107228815", "ปานกลาง"},
        // {"entry.1928392459", "มาก"},
        // {"entry.181684083", "มากที่สุด"},
        // {"entry.1238881123", "มาก"},
        // {"entry.2027028854", "มากที่สุด"},
        // {"entry.1906110760", "มากที่สุด"},
        // {"entry.1014056658", "มาก"},
        // {"entry.1206983443", "มาก"},
        // {"entry.1028681869", "มากที่สุด"},
        // {"entry.464630721", "มาก"},
        // {"entry.1759363537", "มากที่สุด"},
        // {"entry.1750985053", "มาก"},
        // {"entry.1863624926", "มาก"},
        // {"entry.1580336818", "มากที่สุด"},
        // {"entry.1292364023", "มากที่สุด"},
        // {"entry.989663899", "มาก"},
        // {"entry.1129496875", "มากที่สุด"},
        // {"entry.1782330662", "มาก"},
        // {"entry.708788742", "มาก"},
        // {"entry.1524763957", "มากที่สุด"},
        // {"entry.1983630373", "มากที่สุด"},
        // {"pageHistory", "0,1,2,3"}
        //     };

        //     var submitResult = await SubmitToGoogleFormAsync(googleFormLink + "/formResponse", formData);
        //     Console.WriteLine(submitResult);


        var result = await LoadGoogleFormStructureAsync(urlGoogleForm + "/viewform");
        Console.WriteLine(result.FormDocName);
        Dictionary<string, string> formData = new Dictionary<string, string>() { { "pageHistory", "0,1,2,3" } };
        foreach (var i in result.QuestionFieldList)
        {
            Console.WriteLine($"({i.AnswerSubmissionId})" + i.QuestionText + ", Type: " + i.QuestionType);
            formData.Add($"entry.{i.AnswerSubmissionId}", "");
            if (i.AnswerOptionList.Count > 0)
            {
                Console.WriteLine("\tAnswer Option: " + String.Join(",", i.AnswerOptionList));
            }
        }
        // Console.WriteLine("How many submition time you need ? : ");
        // string input = Console.ReadLine();
        // int numSubmit;
        // Int32.TryParse(input, out numSubmit);
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("#### Submiting Answers ####");
        Console.WriteLine();
        Console.WriteLine();
        var random = new Random();
        for (var r = 0; r < 70; r++)
        {
            List<string> answers = new List<string>();
            foreach (var i in result.QuestionFieldList)
            {
                string answer = "";
                int index = 0;
                if (i.AnswerOptionList.Count > 0)
                {
                    if (i.QuestionType == GoogleFormsFieldTypeEnum.GridChoiceField)
                        index = random.Next(1, 3); // Only ["ปากกลาง", "มาก", "มากที่สุด"]
                    else
                        index = random.Next(i.AnswerOptionList.Count);
                    answer = i.AnswerOptionList[index];
                }

                formData[$"entry.{i.AnswerSubmissionId}"] = answer;
                answers.Add(answer);
            }
            var submitResult = await SubmitToGoogleFormAsync(urlGoogleForm + "/formResponse", formData);
            Console.WriteLine($"#{r + 1} Submited({submitResult}): " + String.Join(",", answers));

            // Console.WriteLine($"#${r} Submit Result: " + submitResult);
        }
    }


    public static async Task<bool> SubmitToGoogleFormAsync(string yourGoogleFormsUrl, Dictionary<string, string> formData)
    {
        // Init HttpClient to send the request
        HttpClient client = new HttpClient();

        // Encode object to application/x-www-form-urlencoded MIME type
        var content = new FormUrlEncodedContent(formData);

        // Post the request (replace with your Google Form link)
        var response = await client.PostAsync(
            yourGoogleFormsUrl,
            content);

        if (response.StatusCode == System.Net.HttpStatusCode.OK)
            return true;

        return false;
    }

    public static async Task<GoogleForm> LoadGoogleFormStructureAsync(string yourGoogleFormsUrl)
    {
        HtmlWeb web = new HtmlWeb();
        var htmlDoc = await web.LoadFromWebAsync(yourGoogleFormsUrl).ConfigureAwait(false);

        var htmlNodes = htmlDoc.DocumentNode.SelectNodes("//script").Where(
            x => x.GetAttributeValue("type", "").Equals("text/javascript") &&
                 x.InnerHtml.Contains("FB_PUBLIC_LOAD_DATA_"));

        var fbPublicLoadDataJsScriptContent = htmlNodes.First().InnerHtml;

        // cleaning up "var FB_PUBLIC_LOAD_DATA_ = " at the beginning and 
        // and ";" at the end of the script text  
        var beginIndex = fbPublicLoadDataJsScriptContent.IndexOf("[", StringComparison.Ordinal);
        var lastIndex = fbPublicLoadDataJsScriptContent.LastIndexOf(";", StringComparison.Ordinal);
        var fbPublicJsScriptContentCleanedUp = fbPublicLoadDataJsScriptContent
                                                    .Substring(beginIndex, lastIndex - beginIndex).Trim();

        var jArray = JArray.Parse(fbPublicJsScriptContentCleanedUp);

        GoogleForm googleForm = new GoogleForm();
        googleForm.QuestionFieldList = new List<GoogleFormField>();

        var description = jArray[1][0].ToObject<string>();
        var title = jArray[1][8].ToObject<string>();
        var formId = jArray[14].ToObject<string>();
        var formDocName = jArray[3].ToObject<string>();

        googleForm.Description = description;
        googleForm.Title = title;
        googleForm.FormId = formId;
        googleForm.FormDocName = formDocName;

        var arrayOfFields = jArray[1][1];

        foreach (var field in arrayOfFields)
        {
            // Check if this Field is submittable or not
            // index [4] contains the Field Answer 
            // Submit Id of a Field Object 
            // ex: ignore Fields used as Description panels
            // ex: ignore Image banner fields
            var field4 = field[4].HasValues;
            if (field.Count() < 4 && !field[4].HasValues)
                continue;

            GoogleFormField googleFormField = new GoogleFormField();

            // Load the Question Field data
            var questionTextValue = field[1]; // Get Question Text
            var questionText = questionTextValue.ToObject<string>();

            var questionTypeCodeValue = field[3].ToObject<int>(); // Get Question Type Code   
            var isQuestionTypeExists = Enum.IsDefined(typeof(GoogleFormsFieldTypeEnum), questionTypeCodeValue);
            if (!isQuestionTypeExists) continue;
            var isRecognizedFieldType = Enum.TryParse(questionTypeCodeValue.ToString(),
                                            out GoogleFormsFieldTypeEnum questionTypeEnum);

            if (questionTypeEnum == GoogleFormsFieldTypeEnum.GridChoiceField)
            {
                var fieldSubQuestions = field[4].ToList();
                if (fieldSubQuestions.Count > 1)
                {
                    foreach (var subQuestion in fieldSubQuestions)
                    {
                        var _subQuestionId = subQuestion[0].ToObject<string>();
                        var _subQuestionAnswers = subQuestion[1].ToList();
                        var _isSubQuestionRequired = subQuestion[2].ToObject<int>() == 1;
                        var _subQuestionsName = subQuestion[3][0].ToObject<string>();
                        var answerOptionsList = new List<string>();
                        foreach (var ans in _subQuestionAnswers)
                        {
                            answerOptionsList.Add(ans[0].ToObject<string>());
                        }

                        googleFormField = new GoogleFormField();
                        googleFormField.AnswerOptionList = answerOptionsList;
                        googleFormField.AnswerSubmissionId = _subQuestionId;
                        googleFormField.QuestionText = _subQuestionsName;
                        googleFormField.QuestionType = questionTypeEnum;
                        googleFormField.IsAnswerRequired = _isSubQuestionRequired;

                        googleForm.QuestionFieldList.Add(googleFormField);
                    }
                }
            }
            else
            {
                var answerOptionsList = new List<string>();
                var answerOptionsListValue = field[4][0][1].ToList(); // Get Answers List
                                                                      // List of Answers Available
                var answerSubmitIdValue = field[4][0][0]; // Get Answer Submit Id
                var isAnswerRequiredValue = field[4][0][2]; // Get if Answer is Required to be Submitted
                var answerSubmissionId = answerSubmitIdValue.ToObject<string>();
                var isAnswerRequired = isAnswerRequiredValue.ToObject<int>() == 1 ? true : false; // 1 or 0

                if (answerOptionsListValue.Count > 0)
                {
                    foreach (var answerOption in answerOptionsListValue)
                    {
                        answerOptionsList.Add(answerOption[0].ToString());
                    }
                }

                googleFormField.QuestionText = questionText;
                googleFormField.QuestionType = questionTypeEnum;
                googleFormField.AnswerOptionList = answerOptionsList;
                googleFormField.AnswerSubmissionId = answerSubmissionId;
                googleFormField.IsAnswerRequired = isAnswerRequired;

                googleForm.QuestionFieldList.Add(googleFormField);
            }

        }

        return googleForm;
    }

}