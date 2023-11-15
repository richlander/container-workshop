
using Microsoft.Data.Sqlite;

var dataSource = "hello.db";

var action = args switch
{
    [string name] => LookupName(name),
    ["set", string name] => StoreName(name),
    var command => throw new Exception("unknown command")
};
action?.Invoke();

return 1;

Action LookupName(string name) => () => 
{
    EnsureTable();
    using (var connection = new SqliteConnection($"Data Source={dataSource}"))
    {
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText =
        @"
        SELECT id
        FROM user
        WHERE name = $name
    ";
        command.Parameters.AddWithValue("$name", name);

        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                var id = reader.GetInt32(0);

                Console.WriteLine($"User {name} is {id}");
            }
        }
    }
};

Action StoreName(string name) => () =>
{
    EnsureTable();
    using (var connection = new SqliteConnection($"Data Source={dataSource}"))
    {
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText =
        @"
        INSERT INTO user(name)
        VALUES($name)
        returning id
    ";
        command.Parameters.AddWithValue("$name", name);

        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                var id = reader.GetInt32(0);

                Console.WriteLine($"Inserted {name} as {id}");
            }
        }
    }
};

void EnsureTable()
{
    using (var connection = new SqliteConnection($"Data Source={dataSource}"))
    {
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText =@"create table if not exists user(id integer primary key, name string)";

        command.ExecuteNonQuery();
    }
}