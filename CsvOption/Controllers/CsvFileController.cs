using CsvOption.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.Extensions.Options;
using System.Data;
using System.IO;
using System.Text;


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




            return View("Table",data);



        }
        private List<CsvRow> Readfile(string filePath)
        {
            var rows = new List<CsvRow>();
            using(var reader = new StreamReader(filePath))
            {
                var headers = reader.ReadLine()?.Split(',');
                while(!reader.EndOfStream)
                {
                    var values = reader.ReadLine()?.Split(",");
                    if (values == null || headers == null) continue;
                    var row = new CsvRow();
                    for(int i = 0;i<headers.Length;i++)
                    {
                        row.Fields[headers[i]] = i < values.Length ? values[i] : string.Empty;
                    }
                    rows.Add(row);
                }
            }
            return rows;
            
        }

        //[HttpGet]
        //    public IActionResult Edit(string fileName)
        //    {
        //        var filePath = Path.Combine(env.WebRootPath, uploadfile.UploadFile, fileName);
        //        var data = Readfile(filePath);

        //        ViewBag.FileName = fileName;
        //        return View(data);
        //    }

        //    public IActionResult Edit(string fileName, List<CsvRow> updatedRows)
        //    {
        //        var filePath = Path.Combine(env.WebRootPath, uploadfile.UploadFile, fileName);
        //        var headers = updatedRows.First().Fields.Keys.ToList();

        //        var csvLines = new List<string>
        //{
        //    string.Join(",", headers)
        //};

        //        foreach (var row in updatedRows)
        //        {
        //            csvLines.Add(string.Join(",", headers.Select(h => EscapeCsv(row.Fields[h]))));
        //        }

        //        System.IO.File.WriteAllLines(filePath, csvLines);

        //        // Redirect back to table view after save
        //        return RedirectToAction("Table", new { fileName });

        //    }


        [HttpGet,HttpPost]
        public IActionResult Edit(string fileName, List<CsvRow>? updatedRows)
        {
            var filePath = Path.Combine(env.WebRootPath, uploadfile.UploadFile, fileName);

            if (Request.Method == "POST" && updatedRows != null)
            {
                var headers = updatedRows.First().Fields.Keys.ToList();
                var csvLines = new List<string> { string.Join(",", headers) };

                foreach (var row in updatedRows)
                {
                    csvLines.Add(string.Join(",", headers.Select(h => EscapeCsv(row.Fields[h]))));
                }

                System.IO.File.WriteAllLines(filePath, csvLines);
                return RedirectToAction("Index");
            }

            var data = Readfile(filePath);
            ViewBag.FileName = fileName;
            return View(data);
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
    }
    
}
