using System;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Threading.Tasks;

namespace AsyncDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            // based on: https://learn.microsoft.com/en-us/ef/ef6/fundamentals/async
            bool bLoop = true;

            var context = new BloggingContext();
            var dbContextTransaction = context.Database.BeginTransaction();

            while (bLoop)
            {
                Console.WriteLine("\n\n-------------------MAIN MENU-------------------\n\n");
                Console.WriteLine("S to search\n V to view records\n B to add a new Blog \n A to add posts to a blog \n R to remove a blog and all linked posts\n Enter to Save Changes\n D to view local entities\n BackSpace to rollback changes\n X to exit");

                //Blog? firstBlog = context.Blogs.Find(1);

                ConsoleKey usrInput = Console.ReadKey().Key;
                Console.Clear();
                bLoop = (usrInput != ConsoleKey.X);

                if (bLoop != true) Environment.Exit(0);

                if (usrInput == ConsoleKey.S) RunSearchBlogById(context).Wait();
                else if (usrInput == ConsoleKey.V) DisplayBlogsAndPosts(context).Wait();
                else if (usrInput == ConsoleKey.A) AddPostsTpBlogById(context).Wait();
                else if (usrInput == ConsoleKey.B) AddBlog(context);
                else if (usrInput == ConsoleKey.R) RemoveBlogAndLinkedPosts(context).Wait();
                else if (usrInput == ConsoleKey.D) PrintContextLocalBlogsAndPosts(context);
                else if (usrInput == ConsoleKey.Enter) PrintAndSavePendingChanges(ref context, ref dbContextTransaction);
                else if (usrInput == ConsoleKey.Backspace) RollBackChanges(ref context, ref dbContextTransaction);
                else continue;
            }
        }

        public static async Task AddPostsTpBlogById(BloggingContext context)
        {
            Console.WriteLine("\nType Blog Id then press Enter to Add 10 Posts to it...\n");
            int? usrInt = TryGetUserInputInt();
            if (usrInt == null)
            {
                Console.WriteLine("try again dude");
                return;
            }

            Blog? blog = await context.Blogs.FindAsync(usrInt);
            if (blog == null)
            {
                Console.Clear();
                Console.WriteLine($"\n\n >>>>No Blog with Id: {usrInt}<<<<\n\n");
                return;
            }

            for (int i = 0; i < 10; i++)
            {
                context.Posts.Add(new Post
                {
                    Title = $"post#{i} @ {DateTime.Now}",
                    Content = $"{blog.Name} content #{i}",
                    BlogId = blog.BlogId,
                    Blog = blog
                });
            }

            PrintPendingChanges(context);
        }

        public static async Task RemoveBlogAndLinkedPosts(BloggingContext context)
        {
            Console.WriteLine($"Enter a Id of a blog you want to remove as well as it's linked posts...");
            int? usrInt = TryGetUserInputInt();
            if (usrInt == null)
            {
                Console.WriteLine("try again dude");
                return;
            }

            Blog? blog = await context.Blogs.FindAsync(usrInt);
            if (blog == null)
            {
                Console.WriteLine($"\n\n >>>>No Blog with Id: {usrInt}<<<<\n\n");
                return;
            }

            context.Posts.RemoveRange(blog.Posts);
            context.Blogs.Remove(blog);

            PrintPendingChanges(context);
        }

        public static async Task RunSearchBlogById(BloggingContext context)
        {
            Console.WriteLine("Enter number then press Enter\n");
            int? usrInt = TryGetUserInputInt();
            if (usrInt == null)
            {
                Console.WriteLine("try again dude");
                return;
            }

            Blog? result = await context.Blogs.FindAsync(usrInt);
            if (result == null) Console.WriteLine("none exist bro");
            else
            {
                result.PrintBlog();
                result.PrintPosts();
            }
            return;
        }

        public static async Task DisplayBlogsAndPosts(BloggingContext context)
        {
            //Query for all blogs ordered by Id
            Console.WriteLine("Executing query.");
            var blogs = await (from b in context.Blogs
                               orderby b.BlogId
                               select b).ToListAsync();
            foreach (var blog in blogs)
            {
                blog.PrintBlog();
                blog.PrintPosts();
            }
        }

        public static void AddBlog(BloggingContext context)
        {
            // create a new blog and save it
            context.Blogs.Add(
                new Blog() { Name = $"Test Blog {DateTime.Now}" }
            );

            PrintPendingChanges(context);
        }

        public static void PrintAndSavePendingChanges(ref BloggingContext context, ref DbContextTransaction? transaction)
        {
            PrintPendingChanges(context);

            context.SaveChanges();

            transaction?.Commit();

            transaction = context.Database.BeginTransaction();
        }

        public static void RollBackChanges(ref BloggingContext context, ref DbContextTransaction? transaction)
        {
            if (context == null) return;

            PrintPendingChanges(context);

            transaction?.Rollback();

            context = new BloggingContext();

            transaction = context.Database.BeginTransaction();

            Console.WriteLine("\n\n-------------------RollBack complete-------------------\n\n");

            PrintPendingChanges(context);
        }

        public static int? TryGetUserInputInt()
        {
            string? usrNumber = Console.ReadLine();
            if (usrNumber == null
                || (int.TryParse(usrNumber, out int value) == false))
                return null;

            return value;
        }

        public static void PrintPendingChanges(BloggingContext context)
        {
            // Get some information about just the tracked blogs
            Console.WriteLine("\n>Pending Change Tracked blogs: ");
            foreach (var blog in context.ChangeTracker.Entries<Blog>())
            {
                if (blog == null) continue;
                Console.WriteLine($"Found Blog {blog.Entity.BlogId}: {blog.Entity.Name}, State:{blog.State}");
            };

            Console.WriteLine("\n>Pending Change Tracked Posts: ");
            foreach (var post in context.ChangeTracker.Entries<Post>())
            {
                if (post == null) continue;
                Console.WriteLine($"Post State: {post.State}, {post.Entity.Title}, Content: {post.Entity.Content}");
            }
        }

        public static void PrintContextLocalBlogsAndPosts(BloggingContext context)
        {
            // Loop over the Blogs in the context again.
            Console.WriteLine("\nBlogs In Local after query: ");
            foreach (var blog in context.Blogs.Local)
            {
                Console.WriteLine(
                    "Found {0}: {1} with state {2}",
                    blog.BlogId,
                    blog.Name,
                    context.Entry(blog).State);
            }
            // Loop over the posts in the context again.
            Console.WriteLine("\nIn Local after query: ");
            foreach (var post in context.Posts.Local)
            {
                Console.WriteLine(
                    "Found {0}: {1} with state {2}",
                    post.PostId,
                    post.Title,
                    context.Entry(post).State);
            }
        }
    }
}