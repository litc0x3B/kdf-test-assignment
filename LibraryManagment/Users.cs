namespace LibraryManagment
{

    public class UserCollection
    {
        private Dictionary<string, User> userByEmail = new();

        public void AddUser(User user)
        {
            if (!userByEmail.TryAdd(user.Email, user))
            {
                throw new ArgumentException($"User with email '{user.Email}' already exists");
            }
        }

        public User FindUserByEmail(string email)
        {
            if (!userByEmail.TryGetValue(email, out var user))
            {
                throw new ArgumentException($"No such user with email '{email}'");
            }
            return user;
        }

        public List<User> GetUserList()
        {
            return userByEmail.Select(x => x.Value).ToList();
        }
    }

    public abstract class User(string name, string email)
    {
        public string Name { get; } = name;
        public string Email { get; } = email;
        public int BorrowedBooks { get; private set; }
        public abstract int MaxReturnDays { get; }
        public abstract int MaxBooks { get; }

        internal void Borrow()
        {
            if (BorrowedBooks >= MaxBooks)
            {
                throw new InvalidOperationException($"User isn't allowed to have more than {MaxBooks} book(s)");
            }
            BorrowedBooks++;
        }

        internal void Return()
        {
            if (BorrowedBooks == 0)
            {
                throw new InvalidOperationException("No books were borrowed. Cannot return.");
            }
            BorrowedBooks--;
        }
    }

    public class UserStudent : User
    {
        public UserStudent(string name, string email) : base(name, email)
        {
        }

        public override int MaxReturnDays => 14;
        public override int MaxBooks => 3;
    }

    public class UserFaculty : User
    {
        public UserFaculty(string name, string email) : base(name, email)
        {
        }

        public override int MaxReturnDays => 30;
        public override int MaxBooks => 10;
    }

    public class UserGuest : User
    {
        public UserGuest(string name, string email) : base(name, email)
        {
        }

        public override int MaxReturnDays => 7;
        public override int MaxBooks => 1;
    }
}