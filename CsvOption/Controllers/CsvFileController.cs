using CsvOption.Models;
using DocumentFormat.OpenXml.Bibliography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Data;
using System.IO;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace CsvOption.Controllers
{
    public class CsvFileController : Controller
    {
        private readonly IWebHostEnvironment env;
        private readonly FileUpload uploadfile;

        public CsvFileController(IWebHostEnvironment env, IOptions<FileUpload> uploadfile)
        {
            this.env=env;
            this.uploadfile = uploadfile.Value;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Upload(IFormFile file)
        {
            if(file == null|| file.Length==0)
            {
                ViewBag.Error = "Upload A Valid File";
                return View("Index");
            }
            var folderpath = Path.Combine(env.WebRootPath, uploadfile.UploadFile);//get the file
            if(!Directory.Exists(folderpath))//create file path if not exist
            {
                Directory.CreateDirectory(folderpath);

            }
            var file_path = Path.Combine(folderpath, Path.GetFileName (file.FileName));
            using(var stream = new FileStream(file_path,FileMode.Create))
            {
                file.CopyTo(stream);//To save the file
            }
            var data = Readfile(file_path);
            //List<CsvRow> listdata = data;
            //TempData["FilePath"] = filepath;



            ViewBag.FileName = Path.GetFileName(file.FileName);
            return View("Table",data);



        }
        private List<CsvRow> Readfile(string filePath)
        {
            var lines = System.IO.File.ReadAllLines(filePath);
            var rows = new List<CsvRow>();
            if(lines.Length<0) return rows;//return the empty

            var headers = lines[0].Split(',');
            ViewBag.headers= headers;

            for (int i = 1; i < lines.Length; i++)
            {
                var values = lines[i].Split(",");
                var fields = new Dictionary<string, string>();


                for (int j = 0; j < headers.Length; j++)
                {
                    var header = string.IsNullOrWhiteSpace(headers[j]) ? $"Column{j + 1}" : headers[j];
                    fields[header] = j < values.Length ? values[j] : "";

                }

                rows.Add(new CsvRow { Fields = fields });
            }
            return rows;



            //using(var reader = new StreamReader(filePath))
            //{
            //    var headers = reader.ReadLine()?.Split(',');
            //    while(!reader.EndOfStream)
            //    {
            //        var values = reader.ReadLine()?.Split(",");
            //        if (values == null || headers == null) continue;
            //        var row = new CsvRow();
            //        for(int i = 0;i<headers.Length;i++)
            //        {
            //            row.Fields[headers[i]] = i < values.Length ? values[i] : string.Empty;
            //        }
            //        rows.Add(row);
            //    }
            //}
            //return rows;

        }

        //.................................
        [HttpGet,HttpPost]
        public IActionResult Edit(string fileName, int id, CsvRow? updatedRow)
        {
            var filePath = Path.Combine(env.WebRootPath, uploadfile.UploadFile, fileName);
           
            if (Request.Method == "POST" && updatedRow != null)
            {
                var data = Readfile(filePath);

                var originalRow = data[id];
                if(originalRow.Fields.ContainsKey("transactionDate") && updatedRow.Fields.ContainsKey("transactionDate"))
                {
                    var originalTime = originalRow.Fields["transactionDate"]?.Trim();
                    var newTime = updatedRow.Fields["transactionDate"]?.Trim();

                    if(string.Equals(originalTime, newTime, StringComparison.OrdinalIgnoreCase))
                    {
                        updatedRow.Fields["transactionDate"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                }




                data[id] = updatedRow;

                var headers = data.First().Fields.Keys.ToList();
                var csvLines = new List<string> { string.Join(",", headers) };

                foreach (var row in data)
                {
                    csvLines.Add(string.Join(",", headers.Select(h => EscapeCsv(row.Fields[h]))));
                }

                System.IO.File.WriteAllLines(filePath, csvLines);
                ViewBag.FileName = fileName;
                return View("Table", data);
            }

            var allRows = Readfile(filePath);
            ViewBag.FileName = fileName;
            ViewBag.RowIndex = id;
            return View(allRows[id]);
        }


        private string EscapeCsv(string field)
        {
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
            {
                field = field.Replace("\"", "\"\"");
                return $"\"{field}\"";
            }
            return field;
        }

        //[HttpGet]
        //public IActionResult AddColumn(string filename)
        //{
        //    var filepath = Path.Combine(env.WebRootPath, uploadfile.UploadFile, filename);
        //        var data =Readfile(filepath);
        //    ViewBag.FileName = filename;
        //    return View(data);
        //}



        [HttpGet]
        public IActionResult AddColumn(string fileName)
        {
            var filePath = Path.Combine(env.WebRootPath, uploadfile.UploadFile, fileName);

            var data = ReadCsvWithData(filePath); // custom reader below
            ViewBag.FileName = fileName;
            return View(data);
        }

        [HttpPost]
        public IActionResult AddColumn(string fileName, string newColumnName, List<string> columnValues)
        {
            if (string.IsNullOrWhiteSpace(newColumnName))
            {
                ModelState.AddModelError("", "Column name cannot be empty.");
                var filePath = Path.Combine(env.WebRootPath, uploadfile.UploadFile, fileName);
                var data = ReadCsvWithData(filePath);
                ViewBag.FileName = fileName;
                return View(data);
            }

            var filePathPost = Path.Combine(env.WebRootPath, uploadfile.UploadFile, fileName);
            var dataPost = ReadCsvWithData(filePathPost);

            // Add new column & values
            for (int i = 0; i < dataPost.Count; i++)
            {
                var value = (columnValues != null && i < columnValues.Count) ? columnValues[i] : "";
                dataPost[i].Fields[newColumnName] = value;
            }

            // Save updated CSV
            var headers = dataPost.First().Fields.Keys.ToList();
            var csvLines = new List<string> { string.Join(",", headers) };

            foreach (var row in dataPost)
            {
                csvLines.Add(string.Join(",", headers.Select(h => EscapeCsv(row.Fields[h]))));
            }

            System.IO.File.WriteAllLines(filePathPost, csvLines);

            // Reload updated data and return table
            var updatedData = ReadCsvWithData(filePathPost);
            ViewBag.FileName = fileName;
            return View("Table", updatedData);
        }

        // Helper to read headers + rows
        private List<CsvRow> ReadCsvWithData(string filePath)
        {
            var lines = System.IO.File.ReadAllLines(filePath).ToList();
            if (!lines.Any()) return new List<CsvRow>();

            var headers = lines[0].Split(',').Select(h => h.Trim()).ToList();
            var data = new List<CsvRow>();

            for (int i = 1; i < lines.Count; i++)
            {
                var values = lines[i].Split(',');
                var fields = new Dictionary<string, string>();

                for (int j = 0; j < headers.Count; j++)
                {
                    fields[headers[j]] = j < values.Length ? values[j] : "";
                }

                data.Add(new CsvRow { Fields = fields });
            }

            return data;
        }

        public IActionResult DownloadAsPdf(string fileName)
        {
            if(string.IsNullOrEmpty(fileName))
            {
                return BadRequest("Enter File Name");
            }
            var filePathPost = Path.Combine(env.WebRootPath, uploadfile.UploadFile, fileName);
            if(!System.IO.File.Exists(filePathPost))
            {
                return NotFound("File Not Found");
            }
            var data = Readfile(filePathPost);//reading the file as list(csvrow)
            using (var stream = new MemoryStream())
            {

                //create the pdf and open to enter data from stream
                var document = new iTextSharp.text.Document();
                iTextSharp.text.pdf.PdfWriter.GetInstance(document, stream);
                document.Open();

                var table = new iTextSharp.text.pdf.PdfPTable(data.First().Fields.Count);//make a table with no of columns

                foreach(var header in data.First().Fields.Keys)
                {
                    table.AddCell(new iTextSharp.text.Phrase(header));//adding the headers
                }

                foreach(var row in data)
                {
                    foreach(var value in row.Fields.Values)
                    {
                        table.AddCell(new iTextSharp.text.Phrase(value ?? " "));//reading values to the table
                    }
                }

                document.Add(table);//add the table with all data into the pdf
                document.Close();//close the edit to the pdf, NO changes can make


                return File(stream.ToArray(), "application/pdf", Path.GetFileNameWithoutExtension(fileName) + ".pdf");
            }
        }


        public IActionResult DownloadAsExcel(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return BadRequest("Enter File Name");
            }
            var filePathPost = Path.Combine(env.WebRootPath, uploadfile.UploadFile, fileName);
            if (!System.IO.File.Exists(filePathPost))
            {
                return NotFound("File Not Found");
            }
            var data = Readfile(filePathPost);//reading the file as list(csvrow)


            //creates new excel workbook used ClosedXml library
            using (var book = new ClosedXML.Excel.XLWorkbook()) 
            {
                var Worksheet = book.Worksheets.Add("data");//working with the excel named as data

                //for reading the headers
                var headers = data.First().Fields.Keys.ToList();
                for(int i=0;i<headers.Count;i++)
                {
                    Worksheet.Cell(1, i + 1).Value = headers[i];//excel col starts from 1 not 0

                }
                for(int j=0;j<data.Count;j++)//represents rows
                {
                    int colIndex = 1;//for colums of every row
                    foreach(var value in data[j].Fields.Values)
                    {
                        Worksheet.Cell(j+2,colIndex).Value = value;//j+2 cause 1st row is for headers
                        colIndex++;
                    }
                }
                using(var stream = new MemoryStream())
                {
                    book.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", Path.GetFileNameWithoutExtension(fileName) + ".xlsx");
                }
            }
        }




    }



}
