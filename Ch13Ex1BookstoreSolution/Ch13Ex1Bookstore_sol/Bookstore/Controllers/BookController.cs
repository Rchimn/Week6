using Microsoft.AspNetCore.Mvc;
using Bookstore.Models;
using System.Xml.Linq;

namespace Bookstore.Controllers
{
    public class BookController : Controller
    {
        private Repository<Book> data { get; set; }

        private const string XmlExportPath = "wwwroot/data/books.xml";

        public BookController(BookstoreContext ctx) => data = new Repository<Book>(ctx);

        public RedirectToActionResult Index() => RedirectToAction("List");

        public ViewResult List(BookGridData values)
        {
            // create options for querying books
            var options = new QueryOptions<Book>
            {
                Includes = "Authors, Genre",
                OrderByDirection = values.SortDirection,
                PageNumber = values.PageNumber,
                PageSize = values.PageSize
            };
            if (values.IsSortByGenre)
                options.OrderBy = b => b.GenreId;
            else if (values.IsSortByPrice)
                options.OrderBy = b => b.Price;
            else
                options.OrderBy = b => b.Title;

            // create view model
            var vm = new BookListViewModel
            {
                Books = data.List(options),
                CurrentRoute = values,
                TotalPages = values.GetTotalPages(data.Count)
            };

            return View(vm);
        }

        public ViewResult Details(int id)
        {
            var book = data.Get(new QueryOptions<Book>
            {
                Where = b => b.BookId == id,
                Includes = "Authors, Genre"
            }) ?? new Book();
            return View(book);
        }

        [HttpPost]
        public RedirectToActionResult PageSize(BookGridData currentRoute)
        {
            return RedirectToAction("List", currentRoute.ToDictionary());
        }

        [HttpPost]
        public RedirectToActionResult ExportToXml()
        {
            //load every book with its author and genre
            var options = new QueryOptions<Book>
            {
                Includes = "Authors, Genre",
                OrderBy = b => b.Title
            };
            IEnumerable<Book> books = data.List(options);

            //Build one book element for each record, combine multiple authors names into one string
            XElement booksElement = new XElement("Books");
            foreach (Book book in books)
            {
                string authorNames = string.Empty;
                foreach (Author author in book.Authors)
                {
                    if (authorNames.Length > 0)
                        authorNames += ", ";
                    authorNames += author.FullName;
                }

                booksElement.Add(
                    new XElement("Book",
                        new XElement("ID", book.BookId),
                        new XElement("Title", book.Title),
                        new XElement("Author", authorNames),
                        new XElement("Price", book.Price),
                        new XElement("Genre", book.Genre.Name)
                    )
                );
            }

            XDocument document = new XDocument(booksElement);

            Directory.CreateDirectory(Path.GetDirectoryName(XmlExportPath)!);
            document.Save(XmlExportPath);

            TempData["message"] = "Books exported to XML successfully.";
            return RedirectToAction("List");
        }
    }
}