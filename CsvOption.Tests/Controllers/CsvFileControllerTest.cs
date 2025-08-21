using CsvOption.Controllers;
using CsvOption.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CsvOption.Tests.Controllers
{

    public class CsvFileControllerTest
    {
        CsvFileController controller;
        Mock<IWebHostEnvironment> envmock;
        Mock<IOptions<FileUpload>> optionmock;


        public CsvFileControllerTest()
        {
            envmock = new Mock<IWebHostEnvironment>();
            string temppath = Path.GetTempPath();
            envmock.Setup(e => e.WebRootPath).Returns(temppath);

            optionmock = new Mock<IOptions<FileUpload>>();
            optionmock.SetupGet(e => e.Value).Returns(new FileUpload { UploadFile = "uploads" });
            controller = new CsvFileController(envmock.Object, optionmock.Object);

        }


        //Negetive
        //when no file is select
        [Fact]
        public void Upload_ViewResult_FileIsNull()
        {

            var result = controller.Upload(null);
            Assert.IsType<ViewResult>(result);
        }

        //positive
        //when file is selected
        [Fact]
        public void Upload_ReturnsRedirect_FileIsValid()
        {
            string fileName = "eror.csv";
            string filecontent = "id,name/1,sneha";
            var filebytes = Encoding.UTF8.GetBytes(filecontent);
            var stream = new MemoryStream(filebytes);
            var formfile = new FormFile(stream, 0, filebytes.Length, "file", fileName);

            //act

            var result = controller.Upload(formfile);


            //Assert
            var viewresult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Table", viewresult.ViewName);
            Assert.NotNull(viewresult.Model);
            Assert.Equal(fileName, controller.ViewBag.FileName);
        }

        //Positive case
        //Requesting for edit
        [Fact]
        public void Edit_GetRequest_ReturnsRowView()
        {
            // Arrange
            var filename = "eror.csv";
            var filecontent = "Id,Name\n1,sneha";
            var folderPath = Path.Combine(envmock.Object.WebRootPath
                , "uploads");

            Directory.CreateDirectory(folderPath);

            var filepath = Path.Combine(folderPath, filename);
            File.WriteAllText(filepath, filecontent);

            var controller = new CsvFileController(envmock.Object, optionmock.Object);


            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            controller.ControllerContext.HttpContext.Request.Method = "GET";


            // Act
            var result = controller.Edit(filename, 0, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.True(viewResult.ViewName == null || viewResult.ViewName == "Edit");
            Assert.Equal(filename, controller.ViewBag.FileName);
            Assert.Equal(0, controller.ViewBag.RowIndex);
            Assert.NotNull(viewResult.Model);
        }


        //update confirms
        [Fact]
        public void Edit_PostRequest_UpdateRowReturnTable()
        {
            var filename = "eror.csv";
            var filepath = Path.Combine(envmock.Object.WebRootPath, optionmock.Object.Value.UploadFile, filename);

            Directory.CreateDirectory(Path.GetDirectoryName(filepath));
            File.WriteAllText(filepath, "Id,Name\n1,OldName");


            var updatedRow = new CsvRow { Fields = new Dictionary<string, string> { { "Id", "1" }, { "Name", "NewName" } } };
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            controller.ControllerContext.HttpContext.Request.Method = "POST";



            //Act
            var result = controller.Edit(filename, 0, updatedRow);


            //Assert
            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.True(viewResult.ViewName == null || viewResult.ViewName == "Table");
            // Assert.NotNull(viewResult.Model);
            // Assert.True(viewResult.ViewName == "Table" || string.IsNullOrEmpty(viewResult.ViewName));


            var savedCsv = File.ReadAllText(filepath);
            Assert.Contains("NewName", savedCsv);

        }


        //Negetive Cases
        //test for out of range exception
        [Fact]
        public void Edit_GetRequest_InvalidIndex_Throws()
        {
            // Arrange
            var filename = "EditGetInvalid.csv";
            var folderPath = Path.Combine(envmock.Object.WebRootPath, "uploads");
            Directory.CreateDirectory(folderPath);

            var filepath = Path.Combine(folderPath, filename);
            File.WriteAllText(filepath, "Id,Name\n1,Sneha");

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            controller.ControllerContext.HttpContext.Request.Method = "GET";

            // Act
            Action act = () => controller.Edit(filename, 5, null);

            // Assert
            Assert.Throws<ArgumentOutOfRangeException>(act);
        }


        //confirms same data if no update happened

        [Fact]
        public void Edit_PostRequest_NullRow_DoesNotUpdate()
        {
            // Arrange
            var filename = "edit_post_null.csv";
            var filepath = Path.Combine(envmock.Object.WebRootPath, optionmock.Object.Value.UploadFile, filename);

            Directory.CreateDirectory(Path.GetDirectoryName(filepath));
            File.WriteAllText(filepath, "Id,Name\n1,OldName");

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            controller.ControllerContext.HttpContext.Request.Method = "POST";

            // Act
            var result = controller.Edit(filename, 0, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var savedCsv = File.ReadAllText(filepath);
            Assert.Contains("OldName", savedCsv);   // unchanged
            Assert.DoesNotContain("NewName", savedCsv);
        }




        //To download as Pdf
        //Negetive

        [Fact]
        public void DownloadAsPdf_WithInvalidFile_ReturnsNotFound()
        {
            

            // Act
            var result = controller.DownloadAsPdf("missing.csv");

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }
        

        //positive
        [Fact]
        public void DownloadAsPdf_WithValidFile_ReturnsPdfFile()
        {
            // Arrange
            var controller = new CsvFileController(envmock.Object, optionmock.Object);
            var fileName = "eror.csv";
            var filePath = Path.Combine(envmock.Object.WebRootPath, optionmock.Object.Value.UploadFile, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, "Id,Name\n1,Sneha");

            // Act
            var result = controller.DownloadAsPdf(fileName);

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("application/pdf", fileResult.ContentType);
        }


        //To download as excel sheet
        //Negetive

        [Fact]
        public void DownloadAsExcel_WithInvalidFile_ReturnsNotFound()
        {
            //var controller = new CsvFileController(envmock.Object, optionmock.Object);

            var result = controller.DownloadAsExcel("nofile.csv");

            Assert.IsType<NotFoundObjectResult>(result);
        }
        

        //Positive
        [Fact]
        public void DownloadAsExcel_WithValidFile_ReturnsExcelFile()
        {
            var controller = new CsvFileController(envmock.Object, optionmock.Object);
            var fileName = "excel.csv";
            var filePath = Path.Combine(envmock.Object.WebRootPath, optionmock.Object.Value.UploadFile, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, "Id,Name\n1,John");

            var result = controller.DownloadAsExcel(fileName);

            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileResult.ContentType);
        }


        //adding a new column
        //positive test case
        [Fact]
        public void AddColumn_GetRequest_ReturnsViewWithData()
        {
            // Arrange
            var filename = "addcol_get.csv";
            var filepath = Path.Combine(envmock.Object.WebRootPath, optionmock.Object.Value.UploadFile, filename);
            Directory.CreateDirectory(Path.GetDirectoryName(filepath));
            File.WriteAllText(filepath, "Id,Name\n1,Sneha");

            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            // Act
            var result = controller.AddColumn(filename);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(viewResult.ViewName == null || viewResult.ViewName == "AddColumn");
            Assert.Equal(filename, controller.ViewBag.FileName);
            Assert.NotNull(viewResult.Model);
        }


        [Fact]
        public void AddColumn_PostRequest_AddsNewColumn()
        {
            // Arrange
            var filename = "addcol_post.csv";
            var filepath = Path.Combine(envmock.Object.WebRootPath, optionmock.Object.Value.UploadFile, filename);
            Directory.CreateDirectory(Path.GetDirectoryName(filepath));
            File.WriteAllText(filepath, "Id,Name\n1,Sneha\n2,Rani");

            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            var columnValues = new List<string> { "X", "Y" };

            // Act
            var result = controller.AddColumn(filename, "NewColumn", columnValues);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(viewResult.ViewName == null || viewResult.ViewName == "Table");

            var savedCsv = File.ReadAllText(filepath);
            Assert.Contains("NewColumn", savedCsv);
            Assert.Contains("X", savedCsv);
            Assert.Contains("Y", savedCsv);
        }


        [Fact]
        public void AddColumn_PostRequest_ShortValues_FillsEmpty()
        {
            var filename = "addcol_short.csv";
            var filepath = Path.Combine(envmock.Object.WebRootPath, optionmock.Object.Value.UploadFile, filename);
            Directory.CreateDirectory(Path.GetDirectoryName(filepath));
            File.WriteAllText(filepath, "Id,Name\n1,Sneha\n2,Panda");

            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            var columnValues = new List<string> { "OnlyOne" };

            // Act
            var result = controller.AddColumn(filename, "NewColumn", columnValues);

            // Assert
            var savedCsv = File.ReadAllText(filepath);
            Assert.Contains("NewColumn", savedCsv);
            Assert.Contains("OnlyOne", savedCsv); // first row filled
                                                  
            var lines = File.ReadAllLines(filepath);
            Assert.EndsWith(",", lines[2]);  // last cell empty

        }

        //Negetive case
        [Fact]
        public void AddColumn_PostRequest_EmptyColumnName_ReturnsError()
        {
            var filename = "addcol_empty.csv";
            var filepath = Path.Combine(envmock.Object.WebRootPath, optionmock.Object.Value.UploadFile, filename);
            Directory.CreateDirectory(Path.GetDirectoryName(filepath));
            File.WriteAllText(filepath, "Id,Name\n1,Sneha");

            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            // Act
            var result = controller.AddColumn(filename, "", new List<string>());

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(viewResult.ViewName == null || viewResult.ViewName == "AddColumn");
            Assert.False(controller.ModelState.IsValid);
        }



    }

}
