using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace WebAPI_DapperSample.Controllers
{
    public class PersonController : ApiController
    {
        String connectionString = "Data Source=DESKTOP-H4JOI55;Initial Catalog=TestData;Integrated Security=True";

        [HttpGet]
       // [Route("api/person")]
        public async Task<HttpResponseMessage> GetPersons()
        {
            PersonRepository personRepo = new PersonRepository(connectionString);
            List<Person> persons = null;
            try
            {
                persons = await personRepo.GetPersonsAsync();
                return Request.CreateResponse(HttpStatusCode.OK, persons);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpGet]
        [Route("api/person/{PersonId}")]
        public async Task<HttpResponseMessage> GetPersonsById(int personId)
        {
            PersonRepository personRepo = new PersonRepository(connectionString);
            Person person = null;
          
            try
            {
                // person = await personRepo.GetPersonByIdAsync2(Id);
                person = await personRepo.GetPersonByIdAsync(personId);
                return Request.CreateResponse(HttpStatusCode.OK, person);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
    }
}
