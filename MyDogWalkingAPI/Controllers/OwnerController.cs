using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MyDogWalkingAPI.Models;

namespace MyDogWalkingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OwnerController : ControllerBase
    {

        private readonly IConfiguration _config;

        //constructor
        public OwnerController(IConfiguration config)
        {
            _config = config;
        }
        //computed property
        public SqlConnection Connection
        {
            get
            {
                return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="neighborhoodId">supports neighborhoodName</param>
        /// <param name="include"></param>
        /// <param name="q"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] int? neighborhoodId,
            [FromQuery] string include, 
            [FromQuery] string q)
        {


           if (include != "neighborhoodName")
            {
                var owners = GetAllOwners(neighborhoodId, q);
                return Ok(owners);
            }else
            {
                var owners = GetOwnerWithNeighborhood(neighborhoodId, q);
                return Ok(owners);
            };
        }

        [HttpGet("{id}", Name = "GetOwner")]
        public async Task<IActionResult> Get([FromRoute] int Id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                    SELECT o.Id, o.Name, o.Address, o.NeighborhoodId, o.Address, o.Phone, n.Name AS NeighborhoodName, d.Name AS DogName, d.Breed, d.Id As DogId, d.Notes, d.OwnerId
                    FROM Owner o
                    LEFT JOIN Neighborhood n
                    ON n.Id= o.NeighborhoodId
                    LEFT JOIN Dog d
                    ON d.ownerId = o.Id 
                    WHERE o.Id = @id";

                    cmd.Parameters.Add(new SqlParameter("@id", Id));
                    SqlDataReader reader = cmd.ExecuteReader();

                    Owner owner= null;

                    while (reader.Read())
                    {
                        if  (owner == null)
                        {

                            owner = new Owner
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Name = reader.GetString(reader.GetOrdinal("Name")),
                                Address = reader.GetString(reader.GetOrdinal("Address")),
                                Phone = reader.GetString(reader.GetOrdinal("Phone")),
                                NeighborhoodId = reader.GetInt32(reader.GetOrdinal("NeighborhoodId")),
                                Neighborhood = new Neighborhood
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("NeighborhoodId")),
                                    Name = reader.GetString(reader.GetOrdinal("NeighborhoodName"))
                                },
                                Dogs = new List<Dog>()

                            };

                        }
                        owner.Dogs.Add(new Dog {
                            Id = reader.GetInt32(reader.GetOrdinal("DogId")),
                            Name = reader.GetString(reader.GetOrdinal("DogName")),
                            Breed = reader.GetString(reader.GetOrdinal("Breed")),
                            Notes = reader.GetString(reader.GetOrdinal("Notes")), 
                            OwnerId = reader.GetInt32(reader.GetOrdinal("OwnerId"))

                        });
                    }
                    reader.Close();

                    return Ok(owner);
                }
            }
        }
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Owner owner)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO Owner (Name, Address, Phone, NeighborhoodId)
                                        OUTPUT INSERTED.Id
                                        VALUES (@Name, @Address, @Phone, @NeighborhoodId)";
                    cmd.Parameters.Add(new SqlParameter("@Name", owner.Name));
                    cmd.Parameters.Add(new SqlParameter("@Address", owner.Address));
                    cmd.Parameters.Add(new SqlParameter("@Phone", owner.Phone));
                    cmd.Parameters.Add(new SqlParameter("@NeighborhoodId", owner.NeighborhoodId));

                    int newId = (int)cmd.ExecuteScalar();
                    owner.Id = newId;
                    return CreatedAtRoute("GetOwner", new { id = newId }, owner);
                }
            }
        }



        [HttpPut("{id}")]
        public async Task<IActionResult> Put([FromRoute] int id, [FromBody] Owner owner)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"UPDATE Owner
                                            SET Name = @Name, Address=@Address, Phone = @Phone, NeighborhoodId = @neighborhoodId
                                            WHERE Id = @id";
                        cmd.Parameters.Add(new SqlParameter("@Name", owner.Name));
                        cmd.Parameters.Add(new SqlParameter("@Address", owner.Address));
                        cmd.Parameters.Add(new SqlParameter("@Phone", owner.Phone));
                        cmd.Parameters.Add(new SqlParameter("@neighborhoodId", owner.NeighborhoodId));
                        cmd.Parameters.Add(new SqlParameter("@id", id));

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return new StatusCodeResult(StatusCodes.Status204NoContent);
                        }
                        throw new Exception("No rows affected");
                    }
                }
            }
            catch (Exception)
            {
                if (!OwnerExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"DELETE FROM Owner WHERE Id = @id";
                        cmd.Parameters.Add(new SqlParameter("@id", id));

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return new StatusCodeResult(StatusCodes.Status204NoContent);
                        }
                        throw new Exception("No rows affected");
                    }
                }
            }
            catch (Exception)
            {
                if (!OwnerExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        private List<Owner> GetAllOwners ([FromQuery] int? neighborhoodId, [FromQuery] string q)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                    SELECT o.Id, o.Name, o.Address, o.NeighborhoodId, o.Address, o.Phone
                    FROM Owner o
                    WHERE 1=1";

                    if (neighborhoodId != null)
                    {
                        cmd.CommandText += " AND NeighborhoodId = @neighborhoodId";
                        cmd.Parameters.Add(new SqlParameter("@neighborhoodId", neighborhoodId));
                    }

                    if (q != null)
                    {
                        cmd.CommandText += " AND Name LIKE @Name";
                        cmd.Parameters.Add(new SqlParameter("@Name", "%" + q + "%"));
                    }

                    SqlDataReader reader = cmd.ExecuteReader();

                    List<Owner> owners = new List<Owner>();

                    while (reader.Read())
                    {
                        Owner owner = new Owner
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            Address = reader.GetString(reader.GetOrdinal("Address")),
                            Phone = reader.GetString(reader.GetOrdinal("Phone")),
                            NeighborhoodId = reader.GetInt32(reader.GetOrdinal("NeighborhoodId")),
                        

                        };

                        owners.Add(owner);
                    }
                    reader.Close();

                    return owners;
                }
            }

        }

        private List<Owner> GetOwnerWithNeighborhood([FromQuery] int? neighborhoodId, [FromQuery] string q)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                    SELECT o.Id, o.Name, o.Address, o.NeighborhoodId, o.Address, o.Phone, n.Name AS NeighborhoodName
                    FROM Owner o
                    LEFT JOIN Neighborhood n
                    ON n.Id= o.NeighborhoodId
                    WHERE 1=1";

                    if (neighborhoodId != null)
                    {
                        cmd.CommandText += " AND NeighborhoodId = @neighborhoodId";
                        cmd.Parameters.Add(new SqlParameter("@neighborhoodId", neighborhoodId));
                    }

                    if( q != null)
                    {
                        cmd.CommandText += " AND o.Name LIKE @Name";
                        cmd.Parameters.Add(new SqlParameter("@Name", "%" + q + "%"));
                    }
                    SqlDataReader reader = cmd.ExecuteReader();

                    List<Owner> owners = new List<Owner>();

                    while (reader.Read())
                    {
                        Owner owner = new Owner
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            Address = reader.GetString(reader.GetOrdinal("Address")),
                            Phone = reader.GetString(reader.GetOrdinal("Phone")),
                            NeighborhoodId = reader.GetInt32(reader.GetOrdinal("NeighborhoodId")),
                            Neighborhood = new Neighborhood
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("NeighborhoodId")),
                                Name = reader.GetString(reader.GetOrdinal("NeighborhoodName"))
                            }

                        };

                        owners.Add(owner);
                    }
                    reader.Close();

                    return owners;
                }
            }
        }
        private bool OwnerExists(int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT Id, Name, Address, Phone, NeighborhoodId
                        FROM Owner
                        WHERE Id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));

                    SqlDataReader reader = cmd.ExecuteReader();
                    return reader.Read();
                }
            }
        }
    }
}