using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApplication4.Pages
{
    public class IndexModel : PageModel
    {

        public class User
        {
            public string name { get; set; }
            public string age { get; set; }
            public string email { get; set; }
        }


        public void OnGet()
        {

        }


        [IgnoreAntiforgeryToken]
        public JsonResult OnPostSend([FromBody] User user)
        {

            return new JsonResult(new
            {
                message = "Заявka успешно заполнена",
                name = user.name,
                age = user.age,
                email = user.email
            });

        }
    }
}