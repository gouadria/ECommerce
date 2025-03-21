using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

public class WordHelper
{
    public void CreateWordDocument(string filePath)
    {
        using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
        {
            MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
            mainPart.Document = new Document();
            Body body = new Body();

            Paragraph para = new Paragraph();
            Run run = new Run();
            run.Append(new DocumentFormat.OpenXml.Wordprocessing.Text("تطبيق التجارة الإلكترونية هذا هو منصة شاملة لبيع المنتجات عبر الإنترنت. يتميز التطبيق بواجهة مستخدم جذابة وسهلة الاستخدام، مما يتيح للمستخدمين تصفح المنتجات وشرائها بسهولة. يحتوي التطبيق على الأقسام التالية:"));

            para.Append(run);
            body.Append(para);

            mainPart.Document.Append(body);
            mainPart.Document.Save();
        }
    }
}


