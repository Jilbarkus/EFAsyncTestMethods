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
            using (var context = new BloggingContext())
            {
                using (var dbContextTransaction = context.Database.BeginTransaction())
                {
                    while (bLoop)
                    {
                        Console.WriteLine("NOTHING IS SAVED AUTOMATICALLY, Save with ENTER key\n");
                        Console.WriteLine("S to search\n V to view records\n B to add a new Blog \n A to add posts to a blog \n R to remove a blog and all linked posts\n Enter to Save Changes\n D to view pending unsaved entities\n X to exit");

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
                        else if (usrInput == ConsoleKey.Enter) PrintAndSavePendingChanges(context, dbContextTransaction).Wait();
                        else continue;
                    }
                }
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
                Console.WriteLine($"No Blog with Id: {usrInt}");
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
                Console.WriteLine($"No Blog with Id: {usrInt}");
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

        public static async Task PrintAndSavePendingChanges(BloggingContext context, DbContextTransaction? transaction)
        {
            PrintPendingChanges(context);

            await context.SaveChangesAsync();

            transaction?.Commit();

            transaction = context.Database.BeginTransaction();
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
            Console.WriteLine("\nPending Change Tracked blogs: ");
            foreach (var blog in context.ChangeTracker.Entries<Blog>())
            {
                if (blog == null) continue;
                Console.WriteLine($"Found Blog {blog.Entity.BlogId}: {blog.Entity.Name}, State:{blog.State}");
            };

            Console.WriteLine("\nPending Change Tracked Posts: ");
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

        

        #region old
        //public static async Task RunSearchBlogById()
        //{
        //    using (var context = new BloggingContext())
        //    {
        //        Console.WriteLine("Enter number then press Enter\n");
        //        string? usrNumber = Console.ReadLine();
        //        if (usrNumber == null
        //            || (int.TryParse(usrNumber, out int value) == false))
        //        {
        //            Console.WriteLine("try again dude");
        //            return;
        //        }

        //        Blog? result = await context.Blogs.FindAsync(blogId);
        //        if (result == null) Console.WriteLine("none exist bro");
        //        else result.PrintBlog();
        //        return;
        //    }
        //}

        //public static async Task AddAndDisplayBlogByName()
        //{
        //    using (var db = new BloggingContext())
        //    {
        //        // create a new blog and save it
        //        db.Blogs.Add(
        //            new Blog() { Name = "Test Blog #" + (db.Blogs.Count() + 1) }
        //        );
        //        Console.WriteLine("Calling SaveChanges.");
        //        await db.SaveChangesAsync();
        //        Console.WriteLine("SaveChanges Completed.");

        //        //Query for all blogs ordered by name
        //        Console.WriteLine("Executing query.");
        //        var blogs = await (from b in db.Blogs
        //                     orderby b.Name
        //                     select b).ToListAsync();

        //        // Write all blogs out to Console
        //        Console.WriteLine("Query completed with following results:");
        //        foreach (var blog in blogs)
        //        {
        //            Console.WriteLine(" " + blog.Name);
        //        }
        //    }
        //} 
        #endregion
    }
}