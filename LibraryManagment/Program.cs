using System.Diagnostics;
using System.Text.RegularExpressions;
using LibraryManagment;

UserCollection users = new();
BookCollection books = new();
LibraryRecords library = new();

library.CurrentDate = DateTime.Today;

const string EMAIL = @"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}";
const string ISBN  = @"\d+";

Console.WriteLine("Library CLI. Введите 'help' для списка команд. Для выхода — Ctrl+C.");

while (true)
{
    Console.Write("> ");
    var line = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(line)) continue;

    try
    {
        if (line.Equals("help", StringComparison.OrdinalIgnoreCase))
        {
            PrintHelp();
            continue;
        }

        // --- add book <isbn> "<title>" "<author>" <instances>
        var mAddBook = Regex.Match(line,
            $@"^add\s+book\s+(?<isbn>{ISBN})\s+""(?<title>[^""]+)""\s+""(?<author>[^""]+)""\s+(?<instances>\d+)$",
            RegexOptions.IgnoreCase);
        if (mAddBook.Success)
        {
            var isbn = mAddBook.Groups["isbn"].Value;
            var title = mAddBook.Groups["title"].Value;
            var author = new Author(mAddBook.Groups["author"].Value);
            var instances = int.Parse(mAddBook.Groups["instances"].Value);

            books.AddBook(new Book(isbn, author, title, instances));
            Console.WriteLine($"OK: добавлена книга '{title}' (ISBN {isbn}) экземпляров: {instances}");
            continue;
        }

        // --- remove book <isbn>
        var mRemoveBook = Regex.Match(line,
            $@"^remove\s+book\s+(?<isbn>{ISBN})$",
            RegexOptions.IgnoreCase);
        if (mRemoveBook.Success)
        {
            var isbn = mRemoveBook.Groups["isbn"].Value;
            books.RemoveBook(isbn);
            Console.WriteLine($"OK: удалена книга с ISBN {isbn}");
            continue;
        }

        // --- register <student|faculty|guest> "<name>" <email>
        var mRegister = Regex.Match(line,
            $@"^register\s+(?<type>student|faculty|guest)\s+""(?<name>[^""]+)""\s+(?<email>{EMAIL})$",
            RegexOptions.IgnoreCase);
        if (mRegister.Success)
        {
            var type = mRegister.Groups["type"].Value.ToLowerInvariant();
            var name = mRegister.Groups["name"].Value;
            var email = mRegister.Groups["email"].Value;

            User u = type switch
            {
                "student" => new UserStudent(name, email),
                "faculty" => new UserFaculty(name, email),
                "guest"   => new UserGuest(name, email),
                _ => throw new UnreachableException("Unknown user type")
            };

            users.AddUser(u);
            Console.WriteLine($"OK: зарегистрирован {type} '{name}' <{email}>");
            continue;
        }

        // --- borrow <email> <isbn>
        var mBorrow = Regex.Match(line,
            $@"^borrow\s+(?<email>{EMAIL})\s+(?<isbn>{ISBN})$",
            RegexOptions.IgnoreCase);
        if (mBorrow.Success)
        {
            var email = mBorrow.Groups["email"].Value;
            var isbn = mBorrow.Groups["isbn"].Value;

            var user = users.FindUserByEmail(email);
            var book = books.FindBookByIsbn(isbn);
            var rec = library.BorrowBook(user, book);
            Console.WriteLine($"OK: {user.Name} одолжил(а) '{book.Title}'. Вернуть до {rec.ReturnDueDate:yyyy-MM-dd}");
            continue;
        }

        // --- return <email> <isbn>
        var mReturn = Regex.Match(line,
            $@"^return\s+(?<email>{EMAIL})\s+(?<isbn>{ISBN})$",
            RegexOptions.IgnoreCase);
        if (mReturn.Success)
        {
            var email = mReturn.Groups["email"].Value;
            var isbn = mReturn.Groups["isbn"].Value;

            var user = users.FindUserByEmail(email);
            var book = books.FindBookByIsbn(isbn);
            library.ReturnBook(user, book);
            Console.WriteLine($"OK: '{book.Title}' возвращена пользователем {user.Name}");
            continue;
        }

        // --- search title "<title>"
        var mSearchTitle = Regex.Match(line,
            @"^search\s+title\s+""(?<title>[^""]+)""$",
            RegexOptions.IgnoreCase);
        if (mSearchTitle.Success)
        {
            var title = mSearchTitle.Groups["title"].Value;
            var found = books.FindBooksByTitle(title);
            PrintBooks(found);
            continue;
        }

        // --- search author "<author>"
        var mSearchAuthor = Regex.Match(line,
            @"^search\s+author\s+""(?<author>[^""]+)""$",
            RegexOptions.IgnoreCase);
        if (mSearchAuthor.Success)
        {
            var author = new Author(mSearchAuthor.Groups["author"].Value);
            var found = books.FindBooksByAuthor(author);
            PrintBooks(found);
            continue;
        }

        // --- search isbn <isbn>
        var mSearchIsbn = Regex.Match(line,
            $@"^search\s+isbn\s+(?<isbn>{ISBN})$",
            RegexOptions.IgnoreCase);
        if (mSearchIsbn.Success)
        {
            var isbn = mSearchIsbn.Groups["isbn"].Value;
            var b = books.FindBookByIsbn(isbn);
            PrintBooks(new[] { b });
            continue;
        }

        // --- overdue
        if (Regex.IsMatch(line, @"^overdue$", RegexOptions.IgnoreCase))
        {
            var list = library.GetOverdues();
            if (list.Count == 0)
            {
                Console.WriteLine("Просрочек нет.");
            }
            else
            {
                foreach (var r in list)
                {
                    Console.WriteLine($"{r.User.Name,-20} <{r.User.Email,-25}> | {r.Book.Title,-40} | due: {r.ReturnDueDate:yyyy-MM-dd}");
                }
            }
            continue;
        }

        // --- list books
        if (Regex.IsMatch(line, @"^list\s+books$", RegexOptions.IgnoreCase))
        {
            var list = books.GetBookList();
            PrintBooks(list);
            continue;
        }

        // --- list users
        if (Regex.IsMatch(line, @"^list\s+users$", RegexOptions.IgnoreCase))
        {
            var list = users.GetUserList();
            foreach (var u in list)
            {
                Console.WriteLine($"{u.Name,-20} <{u.Email,-25}> | borrowed: {u.BorrowedBooks}/{u.MaxBooks} | max days: {u.MaxReturnDays}");
            }
            continue;
        }

        // --- date set <yyyy-MM-dd> (для тестов просрочек)
        var mSetDate = Regex.Match(line,
            @"^date\s+set\s+(?<date>\d{4}-\d{2}-\d{2})$",
            RegexOptions.IgnoreCase);
        if (mSetDate.Success)
        {
            library.CurrentDate = DateTime.Parse(mSetDate.Groups["date"].Value);
            Console.WriteLine($"OK: CurrentDate = {library.CurrentDate:yyyy-MM-dd}");
            continue;
        }

        Console.WriteLine("Неправильный синтаксис. Введите 'help' для справки.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: {ex.Message}");
    }
}

static void PrintBooks(IEnumerable<Book> books)
{
    foreach (var b in books)
    {
        Console.WriteLine($"{b.Title,-40} | ISBN: {b.Isbn,-15} | Author: {b.Author.FirstName,-20} | " +
                          $"Available: {(b.IsAvaliable ? "yes" : "no")} ({b.InstanceCount - b.BorrowedCount}/{b.InstanceCount})");
    }
}

static void PrintHelp()
{
    Console.WriteLine("""
Доступные команды:

  add book <isbn> "<title>" "<author>" <instances>
  remove book <isbn>

  register <student|faculty|guest> "<name>" <email>

  borrow <email> <isbn>
  return <email> <isbn>

  search title "<title>"
  search author "<author>"
  search isbn <isbn>

  list books
  list users

  overdue
  date set <yyyy-MM-dd> - для теста overdue

ISBN - только цифры, без пробелов и кавычек (Любой длинны. Вы же не хотите вводить все 13 символов?)
Email - должен быть адресом с правльным форматом.
""");
}
