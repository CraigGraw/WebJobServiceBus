using Microsoft.AspNetCore.Mvc;

namespace APIService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DummyController : ControllerBase
    {
        [HttpGet]
        public string Get()
        {
            return "test response";
        }
    }
}
