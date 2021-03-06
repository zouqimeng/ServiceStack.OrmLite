using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests
{
	[TestFixture]
	public class OrmLiteQueryTests
		: OrmLiteTestBase
	{

		[Test]
		public void Can_GetById_int_from_ModelWithFieldsOfDifferentTypes_table()
		{
            using (var db = OpenDbConnection())
			{
                db.DropAndCreateTable<ModelWithFieldsOfDifferentTypes>();

				var rowIds = new List<int>(new[] { 1, 2, 3 });

                for (var i = 0; i < rowIds.Count; i++)
                    rowIds[i] = (int)db.Insert(ModelWithFieldsOfDifferentTypes.Create(rowIds[i]), selectIdentity: true);

                var row = db.SingleById<ModelWithFieldsOfDifferentTypes>(rowIds[0]);

				Assert.That(row.Id, Is.EqualTo(rowIds[0]));
			}
		}

		[Test]
		public void Can_GetById_string_from_ModelWithOnlyStringFields_table()
		{
			using (var db = OpenDbConnection())
			{
                db.DropAndCreateTable<ModelWithOnlyStringFields>();

				var rowIds = new List<string>(new[] { "id-1", "id-2", "id-3" });

				rowIds.ForEach(x => db.Insert(ModelWithOnlyStringFields.Create(x)));

				var row = db.SingleById<ModelWithOnlyStringFields>("id-1");

				Assert.That(row.Id, Is.EqualTo("id-1"));
			}
		}
		
		[Test]
		public void Can_select_with_filter_from_ModelWithOnlyStringFields_table()
		{
			using (var db = OpenDbConnection())
			{
                db.DropAndCreateTable<ModelWithOnlyStringFields>();

				var rowIds = new List<string>(new[] { "id-1", "id-2", "id-3" });

				rowIds.ForEach(x => db.Insert(ModelWithOnlyStringFields.Create(x)));

				var filterRow = ModelWithOnlyStringFields.Create("id-4");
				filterRow.AlbumName = "FilteredName";

				db.Insert(filterRow);

				var rows = db.Where<ModelWithOnlyStringFields>(new { filterRow.AlbumName });
				var dbRowIds = rows.ConvertAll(x => x.Id);
				Assert.That(dbRowIds, Has.Count.EqualTo(1));
				Assert.That(dbRowIds[0], Is.EqualTo(filterRow.Id));

				rows = db.Where<ModelWithOnlyStringFields>(new { filterRow.AlbumName });
				dbRowIds = rows.ConvertAll(x => x.Id);
				Assert.That(dbRowIds, Has.Count.EqualTo(1));
				Assert.That(dbRowIds[0], Is.EqualTo(filterRow.Id));

				var queryByExample = new ModelWithOnlyStringFields { AlbumName = filterRow.AlbumName };
				rows = db.SelectNonDefaults(queryByExample);
				dbRowIds = rows.ConvertAll(x => x.Id);
				Assert.That(dbRowIds, Has.Count.EqualTo(1));
				Assert.That(dbRowIds[0], Is.EqualTo(filterRow.Id));

                SuppressIfOracle("Oracle provider is not smart enough to substitute ':' for '@' parameter delimiter.");

                rows = db.Select<ModelWithOnlyStringFields>(
                    "SELECT * FROM {0} WHERE {1} = @AlbumName"
                    .Fmt("ModelWithOnlyStringFields".SqlTable(), "AlbumName".SqlColumn()), 
                    new { filterRow.AlbumName });
				dbRowIds = rows.ConvertAll(x => x.Id);
				Assert.That(dbRowIds, Has.Count.EqualTo(1));
				Assert.That(dbRowIds[0], Is.EqualTo(filterRow.Id));
			}
		}

		[Test]
		public void Can_loop_each_with_filter_from_ModelWithOnlyStringFields_table()
		{
			using (var db = OpenDbConnection())
			{
                db.DropAndCreateTable<ModelWithOnlyStringFields>();

				var rowIds = new List<string>(new[] { "id-1", "id-2", "id-3" });

				rowIds.ForEach(x => db.Insert(ModelWithOnlyStringFields.Create(x)));

				var filterRow = ModelWithOnlyStringFields.Create("id-4");
				filterRow.AlbumName = "FilteredName";

				db.Insert(filterRow);

				var dbRowIds = new List<string>();
				var rows = db.WhereLazy<ModelWithOnlyStringFields>(new { filterRow.AlbumName });
				foreach (var row in rows)
				{
					dbRowIds.Add(row.Id);
				}

				Assert.That(dbRowIds, Has.Count.EqualTo(1));
				Assert.That(dbRowIds[0], Is.EqualTo(filterRow.Id));
			}
		}

        [Test]
        public void Can_GetSingle_with_filter_from_ModelWithOnlyStringFields_table()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<ModelWithOnlyStringFields>();

                var rowIds = new List<string>(new[] { "id-1", "id-2", "id-3" });

                rowIds.ForEach(x => db.Insert(ModelWithOnlyStringFields.Create(x)));

                var filterRow = ModelWithOnlyStringFields.Create("id-4");
                filterRow.AlbumName = "FilteredName";

                db.Insert(filterRow);

                var row = db.Single<ModelWithOnlyStringFields>(new { filterRow.AlbumName });
                Assert.That(row.Id, Is.EqualTo(filterRow.Id));

                row = db.Single<ModelWithOnlyStringFields>(new { filterRow.AlbumName });
                Assert.That(row.AlbumName, Is.EqualTo(filterRow.AlbumName));

                row = db.Single<ModelWithOnlyStringFields>(new { AlbumName = "Junk", Id = (object)null });
                Assert.That(row, Is.Null);
            }
        }

        class Note
        {
            [AutoIncrement] // Creates Auto primary key
            public int Id { get; set; }

            public string SchemaUri { get; set; }
            public string NoteText { get; set; }
            public DateTime? LastUpdated { get; set; }
            public string UpdatedBy { get; set; }
        }

        [Test]
        public void Can_query_where_and_select_Notes()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Note>();

                var note = new Note
                        {
                            SchemaUri = "tcm:0-0-0",
                            NoteText = "Hello world 5",
                            LastUpdated = new DateTime(2013, 1, 5),
                            UpdatedBy = "RC"
                        };
                note.Id = (int)db.Insert(note, selectIdentity: true);

                var notes = db.Where<Note>(new { SchemaUri = "tcm:0-0-0" });
                Assert.That(notes[0].Id, Is.EqualTo(note.Id));
                Assert.That(notes[0].NoteText, Is.EqualTo(note.NoteText));

                notes = db.SelectFmt<Note>("SchemaUri".SqlColumn() + "={0}", "tcm:0-0-0");
                Assert.That(notes[0].Id, Is.EqualTo(note.Id));
                Assert.That(notes[0].NoteText, Is.EqualTo(note.NoteText));

                SuppressIfOracle("Oracle provider is not smart enough to substitute ':' for '@' parameter delimiter.");

                notes = db.Select<Note>("SELECT * FROM Note WHERE {0}=@schemaUri".Fmt("SchemaUri".SqlColumn()), new { schemaUri = "tcm:0-0-0" });
                Assert.That(notes[0].Id, Is.EqualTo(note.Id));
                Assert.That(notes[0].NoteText, Is.EqualTo(note.NoteText));

                notes = db.Select<Note>("SchemaUri".SqlColumn() + "=@schemaUri", new { schemaUri = "tcm:0-0-0" });
                Assert.That(notes[0].Id, Is.EqualTo(note.Id));
                Assert.That(notes[0].NoteText, Is.EqualTo(note.NoteText));
            }            
        }

        class NoteDto
        {
            public int Id { get; set; }
            public string SchemaUri { get; set; }
            public string NoteText { get; set; }
        }

        [Test]
        public void Can_select_NotesDto_with_pretty_sql()
        {
            using (var db = OpenDbConnection())
            {
                db.DropAndCreateTable<Note>();

                db.Insert(new Note
                {
                    SchemaUri = "tcm:0-0-0",
                    NoteText = "Hello world 5",
                    LastUpdated = new DateTime(2013, 1, 5),
                    UpdatedBy = "RC"
                });

                SuppressIfOracle("Oracle provider is not smart enough to substitute ':' for '@' parameter delimiter.");

                var sql = @"
SELECT
Id, {0}, {1}
FROM {2}
WHERE {0}=@schemaUri
".Fmt("SchemaUri".SqlColumn(), "NoteText".SqlColumn(), "Note".SqlTable());

                var notes = db.Select<NoteDto>(sql, new { schemaUri = "tcm:0-0-0" });
                Assert.That(notes[0].Id, Is.EqualTo(1));
                Assert.That(notes[0].NoteText, Is.EqualTo("Hello world 5"));
            }
        }

        class CustomerDto
        {
            public int CustomerId { get; set; }
            public string @CustomerName { get; set; }
            public DateTime Customer_Birth_Date { get; set; }
        }

        [Test]
        [TestCase("customer_id", "customer_name", "customer_birth_date")]
        [TestCase("customerid%", "@customername", "customer_b^irth_date")]
        [TestCase("customerid_%", "@customer_name", "customer$_birth_#date")]
        [TestCase("c!u@s#t$o%m^e&r*i(d_%", "__cus_tomer__nam_e__", "~cus^tomer$_birth_#date")]
        [TestCase("t030CustomerId", "t030CustomerName", "t030Customer_birth_date")]
        [TestCase("t030_customer_id", "t030_customer_name", "t130_customer_birth_date")]
        [TestCase("t030#Customer_I#d", "t030CustomerNa$^me", "t030Cust^omer_birth_date")]
        public void Can_query_CustomerDto_and_map_db_fields_not_identical_by_guessing_the_mapping(string field1Name, string field2Name, string field3Name)
        {
            SuppressIfOracle("Oracle provider is not smart enough to insert 'from dual' everywhere required in user supplied SQL");

            using (var db = OpenDbConnection())
            {
                var sql = string.Format(@"
                    SELECT 1 AS {0}, 'John' AS {1}, '1970-01-01' AS {2}
                    UNION ALL
                    SELECT 2 AS {0}, 'Jane' AS {1}, '1980-01-01' AS {2}",
                        field1Name.SqlColumn(),
                        field2Name.SqlColumn(),
                        field3Name.SqlColumn());

                var customers = db.Select<CustomerDto>(sql);

                Assert.That(customers.Count, Is.EqualTo(2));

                Assert.That(customers[0].CustomerId, Is.EqualTo(1));
                Assert.That(customers[0].CustomerName, Is.EqualTo("John"));
                Assert.That(customers[0].Customer_Birth_Date, Is.EqualTo(new DateTime(1970, 01, 01)));

                Assert.That(customers[1].CustomerId, Is.EqualTo(2));
                Assert.That(customers[1].CustomerName, Is.EqualTo("Jane"));
                Assert.That(customers[1].Customer_Birth_Date, Is.EqualTo(new DateTime(1980, 01, 01)));
            }
        }
    }
}