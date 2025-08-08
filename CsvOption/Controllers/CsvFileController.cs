using CsvOption.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Data;
using System.IO;


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
            var filepath = Path.Combine(env.WebRootPath, uploadfile.UploadFile);//get the file
            if(!Directory.Exists(filepath))//create file path if not exist
            {
                Directory.CreateDirectory(filepath);

            }
            var file_path = Path.Combine(filepath,Path.GetFileName (file.FileName));
            using(var stream = new FileStream(file_path,FileMode.Create))
            {
                file.CopyTo(stream);//To save the file
            }
            var data = Readfile(filepath);
            //List<CsvRow> listdata = data;
            //TempData["FilePath"] = filepath;




            return View("table",data);



        }
        private DataTable Readfile(string filePath)
        {
            var dt = new DataTable();
            var lines = System.IO.File.ReadAllLines(filePath);

            if (lines.Length > 0)
            {
                var headers = lines[0].Split(",");
                foreach (var header in headers)
                    dt.Columns.Add(header.Trim());

                for (int i = 1; i < lines.Length; i++)
                    dt.Rows.Add(lines[i].Split(","));
            }

            return dt;
        }

        private List<CsvRow> ConvertToList(DataTable table)
        {
            var list = new List<CsvRow>();
            foreach (DataRow row in table.Rows)
            {
                var dict = new Dictionary<string, string>();
                foreach (DataColumn col in table.Columns)
                {
                    dict[col.ColumnName] = row[col]?.ToString() ?? string.Empty;
                }
                list.Add(new CsvRow { Fields = dict });
            }
            return list;
        }
    }
}
