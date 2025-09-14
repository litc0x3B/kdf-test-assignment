using System.Collections.Immutable;

namespace LibraryManagment
{
    public class BookCollection
    {
        private Dictionary<string, Book> BooksByIsbn = new();
        private Dictionary<string, HashSet<Book>> BooksByTitle = new();
        private Dictionary<Author, HashSet<Book>> BooksByAuthor = new();

        public void AddBook(Book book)
        {
            if (BooksByIsbn.ContainsKey(book.Isbn))
            {
                throw new ArgumentException($"Book with ISBN {book.Isbn} already exists");
            }
            BooksByIsbn.Add(book.Isbn, book);
            if (!BooksByTitle.ContainsKey(book.Title))
            {
                BooksByTitle.Add(book.Title, [book]);
            }
            else
            {
                BooksByTitle[book.Title].Add(book);
            }
            if (!BooksByAuthor.ContainsKey(book.Author))
            {
                BooksByAuthor.Add(book.Author, [book]);
            }
            else
            {
                BooksByAuthor[book.Author].Add(book);
            }
        }

        public void RemoveBook(string isbn)
        {
            if (!BooksByIsbn.TryGetValue(isbn, out var book))
            {
                throw new ArgumentException($"No such book with ISBN '{isbn}'");
            }
            BooksByIsbn.Remove(book.Isbn);
            BooksByTitle[book.Title].Remove(book);
            BooksByAuthor[book.Author].Remove(book);
        }

        public Book FindBookByIsbn(string isbn)
        {
            if (!BooksByIsbn.TryGetValue(isbn, out var book))
            {
                throw new NotImplementedException($"No such book with ISBN '{isbn}'");
            }
            return book;
        }

        public HashSet<Book> FindBooksByTitle(string title)
        {
            if (!BooksByTitle.TryGetValue(title, out var books))
            {
                throw new NotImplementedException($"No such books with title '{title}'");
            }
            return books;
        }

        public HashSet<Book> FindBooksByAuthor(Author author)
        {
            if (!BooksByAuthor.TryGetValue(author, out var books))
            {
                throw new NotImplementedException($"No such books with author '{author}'");
            }
            return books;
        }

        public List<Book> GetBookList()
        {
            return BooksByIsbn.Select(x => x.Value).ToList();
        }
    }

    public class LibraryRecords
    {

        private class BorrowingRecordComparer : IComparer<BorrowingRecord>
        {
            public int Compare(BorrowingRecord? x, BorrowingRecord? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return 0;
                }
                if (x is null)
                {
                    return -1;
                }
                if (y is null)
                {
                    return 1;
                }
                int cmp = x.ReturnDueDate.CompareTo(y.ReturnDueDate);
                if (cmp != 0)
                {
                    return cmp;
                }
                cmp = string.Compare(x.User.Email, y.User.Email, StringComparison.OrdinalIgnoreCase);
                if (cmp != 0)
                {
                    return cmp;
                }
                return string.Compare(x.Book.Isbn, y.Book.Isbn, StringComparison.OrdinalIgnoreCase);
            }
        }

        //for testing purposes, obviously
        public DateTime CurrentDate { get; set; }
        private SortedSet<BorrowingRecord> sortedRecords = new(new BorrowingRecordComparer());
        private Dictionary<(User user, Book book), BorrowingRecord> recordsByUserBook = new();

        public BorrowingRecord BorrowBook(User user, Book book)
        {
            book.Borrow();
            try
            {
                user.Borrow();
            }
            catch (InvalidOperationException e)
            {
                book.Return();
                throw;
            }
            var record = new BorrowingRecord(CurrentDate, user, book);
            sortedRecords.Add(record);
            recordsByUserBook.Add((record.User, record.Book), record);
            return record;
        }

        public void ReturnBook(User user, Book book)
        {
            if (!recordsByUserBook.TryGetValue((user, book), out var record))
            {
                throw new KeyNotFoundException($"No borrowing record were found for user '{user.Name}'and book {book.Title}");
            }
            recordsByUserBook.Remove((user, book));
            sortedRecords.Remove(record);
            book.Return();
            try
            {
                user.Return();
            }
            catch (InvalidOperationException e) //not really possible
            {
                book.Borrow();
                throw;
            }
        }

        public List<BorrowingRecord> GetOverdues()
        {
            return sortedRecords.TakeWhile(x => x.ReturnDueDate < CurrentDate).ToList();
        }
    }

    public class BorrowingRecord
    {
        public DateTime BorrowDate { get; }
        public DateTime ReturnDueDate { get; }
        public Book Book { get; }
        public User User { get; }
        internal BorrowingRecord(DateTime date, User user, Book book)
        {
            this.BorrowDate = date;
            this.Book = book;
            this.User = user;
            this.ReturnDueDate = date.AddDays(User.MaxReturnDays);
        }
    }

    public record Author(string FirstName);

    public class Book
    {
        public string Isbn {get;}
        public Author Author {get;}
        public string Title {get;}
        public int InstanceCount {get;}
        public int BorrowedCount { get; private set; } = 0;
        public bool IsAvaliable => BorrowedCount < InstanceCount;

        public Book(string isbn, Author author, string title, int instanceCount)
        {
            this.Isbn = isbn ?? throw new ArgumentNullException(nameof(isbn));
            this.Author = author ?? throw new ArgumentNullException(nameof(author));
            this.Title = title ?? throw new ArgumentNullException(nameof(title));
            this.InstanceCount = instanceCount >= 1 ? instanceCount : throw new ArgumentException("Instance count should be more than zero");
        }

        internal void Borrow()
        {
            if (!IsAvaliable)
            {
                throw new InvalidOperationException("No books avaliable");
            }
            BorrowedCount++;
        }

        internal void Return()
        {
            if (BorrowedCount == 0)
            {
                throw new InvalidOperationException("No books were borrowed. Cannot return.");
            }
            BorrowedCount--;
        }

        public void AddInstances(int instances)
        {
            throw new NotImplementedException();
        }
        public void RemoveInstances(int instances)
        {
            throw new NotImplementedException();
        }
    } 
}