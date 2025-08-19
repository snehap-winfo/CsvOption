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

        [Fact]
        public void Upload_ViewResult_FileIsNull()
        {

            var result = controller.Upload(null);
            Assert.IsType<ViewResult>(result);
        }

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


        [Fact]

        
        public void Edit_GetRequest_ReturnsRowView()
        {
            // Arrange
            optionmock.SetupGet(e => e.Value).Returns(new FileUpload { UploadFile = "uploads" });

            var filename = "error.csv";
            var csvcontent = "Id,Name\n1,Sneha"; // correct CSV with header + row

            var filePath = Path.Combine(envmock.Object.WebRootPath, optionmock.Object.Value.UploadFile, filename);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, csvcontent);

            // Act
            var result = controller.Edit(filename, 0, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.Model);  // model = allRows[0]
            Assert.Equal(0, controller.ViewBag.RowIndex);
            Assert.Equal(filename, controller.ViewBag.FileName);
        }





        /*[Fact]
public void Edit_GetRequest_ReturnsRowView()
{
    // Arrange
    var controller = new CsvFileController(_envMock.Object, _options);
    var fileName = "edit.csv";
    var csvContent = "Id,Name\n1,OldName";
    var filePath = Path.Combine(_envMock.Object.WebRootPath, _options.Value.UploadFile, fileName);
    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
    File.WriteAllText(filePath, csvContent);

    // Act
    var result = controller.Edit(fileName, 0, null);

    // Assert
    var viewResult = Assert.IsType<ViewResult>(result);
    Assert.NotNull(viewResult.Model);
    Assert.Equal(fileName, controller.ViewBag.FileName);
}

[Fact]
public void Edit_PostRequest_UpdatesRowAndReturnsTableView()
{
    // Arrange
    var controller = new CsvFileController(_envMock.Object, _options);
    var fileName = "editpost.csv";
    var csvContent = "Id,Name\n1,OldName";
    var filePath = Path.Combine(_envMock.Object.WebRootPath, _options.Value.UploadFile, fileName);
    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
    File.WriteAllText(filePath, csvContent);

    var updatedRow = new CsvRow { Fields = new Dictionary<string, string> { { "Id", "1" }, { "Name", "NewName" } } };

    controller.ControllerContext = new ControllerContext
    {
        HttpContext = new DefaultHttpContext()
    };
    controller.ControllerContext.HttpContext.Request.Method = "POST";

    // Act
    var result = controller.Edit(fileName, 0, updatedRow);

    // Assert
    var viewResult = Assert.IsType<ViewResult>(result);
    Assert.Equal("Table", viewResult.ViewName);
    var savedCsv = File.ReadAllText(filePath);
    Assert.Contains("NewName", savedCsv);
}
*/



    }

}
