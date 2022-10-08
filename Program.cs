using GoogleFormsToolkitLibrary.Models;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

internal class Program
{
    private static async Task Main(string[] args)
    {

        var googleFormLink =
        "https://docs.google.com/forms/u/0/d/e/1FAIpQLSexhJuAyR-My0nyD3_MC8p9lDPe40K5SZcjc1BoGv8C4AVbdA/viewform";

        var googleTest = "https://docs.google.com/forms/d/15RKb_QdT6TyWHX6ltmOXIGwOLjHUqJYpgc0zj60aL_E";
        var result = await LoadGoogleFormStructureAsync(googleTest);
        Console.WriteLine(result.FormDocName);
        Console.WriteLine("How many submition time you need ? : ");
        string input = Console.ReadLine();
        int numSubmit;
        Int32.TryParse(input, out numSubmit);

        var random = new Random();
        for (var r = 0; r < numSubmit; r++)
        {
            Dictionary<string, string> formData = new Dictionary<string, string>();
            foreach (var i in result.QuestionFieldList)
            {
                // Console.WriteLine($"({i.AnswerSubmissionId})" + i.QuestionText + ", Type: " + i.QuestionType);
                string answer = null;
                if (i.AnswerOptionList.Count > 0)
                {
                    // Console.WriteLine("\tAnswer Option: " + String.Join(",", i.AnswerOptionList));
                    int index = random.Next(i.AnswerOptionList.Count);
                    answer = i.AnswerOptionList[index];
                }

                //Add FormData
                formData.Add($"entry.{i.AnswerSubmissionId}", answer);
            }
            var submitResult = await SubmitToGoogleFormAsync(googleTest + "/formResponse", formData);
            Console.WriteLine($"#${r} Submit Result: " + submitResult);
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

            var answerOptionsList = new List<string>();
            var answerOptionsListValue = field[4][0][1].ToList(); // Get Answers List
                                                                  // List of Answers Available
            if (answerOptionsListValue.Count > 0)
            {
                foreach (var answerOption in answerOptionsListValue)
                {
                    answerOptionsList.Add(answerOption[0].ToString());
                }
            }

            var answerSubmitIdValue = field[4][0][0]; // Get Answer Submit Id
            var isAnswerRequiredValue = field[4][0][2]; // Get if Answer is Required to be Submitted
            var answerSubmissionId = answerSubmitIdValue.ToObject<string>();
            var isAnswerRequired = isAnswerRequiredValue.ToObject<int>() == 1 ? true : false; // 1 or 0

            googleFormField.QuestionText = questionText;
            googleFormField.QuestionType = questionTypeEnum;
            googleFormField.AnswerOptionList = answerOptionsList;
            googleFormField.AnswerSubmissionId = answerSubmissionId;
            googleFormField.IsAnswerRequired = isAnswerRequired;

            googleForm.QuestionFieldList.Add(googleFormField);
        }

        return googleForm;
    }

}