using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncDemo
{
    public static class Extensions
    {
        public static void PrintBlog(this Blog blog)
        {
            if (blog == null) return;
            Console.WriteLine($"Blog ID: {blog.BlogId}, Name: {blog.Name}");
        }

        public static void PrintPosts(this Blog blog)
        {
            if (blog == null ||
                blog.Posts == null) return;
            foreach (Post? post in blog.Posts)
            {
                if (post == null) continue;
                Console.WriteLine($"Post Id:{post.PostId}, Content: {post.Content}");
            }
        }
    }
}
