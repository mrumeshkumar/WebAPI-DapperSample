using System;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using System.Collections;
using System.Collections.Generic;
using System.Web.Http.ExceptionHandling;

namespace WebAPI_DapperSample
{
    public class AsyncDapperDemo
    {
    }
	//public class Program
	//{
	//	public static void Main()
	//	{
	//		var connectionString = "your connection string";
	//		PersonRepository personRepo = new PersonRepository(connectionString);
	//		Person person = null;
	//		Guid Id = new Guid("{82B31BB2-85BF-480F-8927-BB2AB71CE2B3}");

	//		// Typically, you'd be doing this inside of an async Web API controller, not the main method of a console app.
	//		// So, we're just using Task.Factory to simulate an async Web API call.
	//		var task = new Task(async () =>
	//		{
	//			person = await personRepo.GetPersonById(Id);
	//		});

	//		// This just prevents the console app from exiting before the async work completes.
	//		Task.WaitAll(task);
	//	}
	//}

	// Just a simple POCO model
	public class Person
	{
		public Int32 Id { get; set; }
		public string Name { get; set; }
		public string Phone { get; set; }
		public string Email { get; set; }
	}

	// Yes, I know this doesn't fit definition of a generic repository, 
	// but the assumption is that I have no idea how you want to get 
	// your data. That's up to you. This Base repo exists for the 
	// sole purpoose of providing SQL connection management.
	public abstract class BaseRepository
	{
		public readonly string _ConnectionString;
		protected BaseRepository(string connectionString)
		{
			_ConnectionString = connectionString;
		}



		protected IDbConnection GetConnection()
		{
			try
			{
				var connection = new SqlConnection(_ConnectionString);
				connection.Open();
				return connection;

			}
			catch (TimeoutException ex)
			{
				throw new Exception(String.Format("{0}.WithConnection() experienced a SQL timeout", GetType().FullName), ex);
			}
			catch (SqlException ex)
			{
				throw new Exception(String.Format("{0}.WithConnection() experienced a SQL exception (not a timeout)", GetType().FullName), ex);
			}
		}
		protected async Task<IDbConnection> GetConnectionAsync()
		{
			try
			{
				var connection = new SqlConnection(_ConnectionString);
				await connection.OpenAsync();
				return connection;

			}
			catch (TimeoutException ex)
			{
				throw new Exception(String.Format("{0}.WithConnection() experienced a SQL timeout", GetType().FullName), ex);
			}
			catch (SqlException ex)
			{
				throw new Exception(String.Format("{0}.WithConnection() experienced a SQL exception (not a timeout)", GetType().FullName), ex);
			}
		}
		// use for buffered queries that return a type
		protected async Task<T> WithConnectionAsync<T>(Func<IDbConnection, Task<T>> getData)
		{
			try
			{
				using (var connection = new SqlConnection(_ConnectionString))
				{
					await connection.OpenAsync();
					return await getData(connection);
				}
			}
			catch (TimeoutException ex)
			{
				throw new Exception(String.Format("{0}.WithConnection() experienced a SQL timeout", GetType().FullName), ex);
			}
			catch (SqlException ex)
			{
				throw new Exception(String.Format("{0}.WithConnection() experienced a SQL exception (not a timeout)", GetType().FullName), ex);
			}
		}
		// use for buffered queries that do not return a type
		protected async Task WithConnectionAsync(Func<IDbConnection, Task> getData)
		{
			try
			{
				using (var connection = new SqlConnection(_ConnectionString))
				{
					await connection.OpenAsync();
					await getData(connection);
				}
			}
			catch (TimeoutException ex)
			{
				throw new Exception(String.Format("{0}.WithConnection() experienced a SQL timeout", GetType().FullName), ex);
			}
			catch (SqlException ex)
			{
				throw new Exception(String.Format("{0}.WithConnection() experienced a SQL exception (not a timeout)", GetType().FullName), ex);
			}
		}

		// use for non-buffered queries that return a type
		protected async Task<TResult> WithConnection<TRead, TResult>(Func<IDbConnection, Task<TRead>> getData, Func<TRead, Task<TResult>> process)
		{
			try
			{
				using (var connection = new SqlConnection(_ConnectionString))
				{
					await connection.OpenAsync();
					var data = await getData(connection);
					return await process(data);
				}
			}
			catch (TimeoutException ex)
			{
				throw new Exception(String.Format("{0}.WithConnection() experienced a SQL timeout", GetType().FullName), ex);
			}
			catch (SqlException ex)
			{
				throw new Exception(String.Format("{0}.WithConnection() experienced a SQL exception (not a timeout)", GetType().FullName), ex);
			}
		}


	}

	public class PersonRepository : BaseRepository
	{
		public PersonRepository(string connectionString) : base(connectionString)
		{
		}

		// Assumes you have a Person table in your DB that 
		// aligns with the Person POCO model.
		//
		// Assumes you have an exsiting SQL sproc in your DB 
		// with @Id UNIQUEIDENTIFIER as a parameter. The sproc 
		// returns rows from the Person table.
		public async Task<Person> GetPersonByIdAsync(int Id)
		{
			try
			{
				return await WithConnectionAsync(async c =>
				{
					var p = new DynamicParameters();
					p.Add("Id", Id, DbType.Int32);
					var people = await c.QueryAsync<Person>(sql: "sp_Person_GetById", param: p, commandType: CommandType.StoredProcedure);
					return people.FirstOrDefault();
				});
			}
			catch (Exception)
			{

				throw;
			}

		}
		public async Task<List<Person>> GetPersonsAsync()
		{
			try
			{
				return await WithConnectionAsync(async c =>
				{
					var people = await c.QueryAsync<Person>(sql: "sp_Person_GetALL",  commandType: CommandType.StoredProcedure);
					return people.ToList();
				});
			}
			catch (Exception ex)
			{

				throw new Exception (ex.Message);
			}

		}

		public async Task<Person> GetPersonByIdAsync2(int Id)
		{
			using (var connection = new SqlConnection(_ConnectionString))
			{
				try
				{
					await connection.OpenAsync();

					var p = new DynamicParameters();
					p.Add("Id", Id, DbType.Int32);
					var people = await connection.QueryAsync<Person>(sql: "sp_Person_GetById", param: p, commandType: CommandType.StoredProcedure);
					return people.FirstOrDefault();
				}
				catch (TimeoutException ex)
				{
					throw new Exception(String.Format("{0}.WithConnection() experienced a SQL timeout", GetType().FullName), ex);
				}
				catch (SqlException ex)
				{
					throw new Exception(String.Format("{0}.WithConnection() experienced a SQL exception (not a timeout)", GetType().FullName), ex);
				}
				catch (Exception ex)
				{
					throw new Exception(ex.Message.ToString());
				}
			}
		}
		public Person GetPersonByIdV1(int Id)
		{
			var connection = new SqlConnection(_ConnectionString);

			try
			{
				connection.Open(); // synchronously open a connection to the database 

				var p = new DynamicParameters();
				p.Add("Id", Id, DbType.Int32);
				var people = connection.Query<Person>(
					sql: "sp_Person_GetById",
					param: p,
					commandType: CommandType.StoredProcedure);
				return people.FirstOrDefault();
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message.ToString());
			}
			finally
			{
				// Close the connection explicitly, to make sure it gets closed.
				// Otherwise, we might start leaking connections.
				connection.Close();
			}
		}
		public Person GetPersonByIdV1_1(int Id)
		{
			using (var connection = new SqlConnection(_ConnectionString))
			{
				try
				{
					connection.Open(); // synchronously open a connection to the database 

					var p = new DynamicParameters();
					p.Add("Id", Id, DbType.Int32);
					var people = connection.Query<Person>(
						sql: "sp_Person_GetById",
						param: p,
						commandType: CommandType.StoredProcedure);
					return people.FirstOrDefault();
				}
				catch (Exception ex)
				{
					throw new Exception(ex.Message.ToString());
				}
			}
		}

		public Person GetPersonByIdV2(int Id)
		{
			try
			{
				var connection = GetConnection();

				var p = new DynamicParameters();
				p.Add("Id", Id, DbType.Int32);
				var people = connection.Query<Person>(sql: "sp_Person_GetById", param: p, commandType: CommandType.StoredProcedure);
				return people.FirstOrDefault();
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message.ToString());
			}

		}
	}
}