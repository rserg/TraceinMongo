using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;

namespace TraceinMongo
{
    class MongoData
    {
        private string dbname;
        public MongoData(string databasename)
        {
            dbname = databasename;
        }

        //Функциоональные структуры с 81
        public MongoCollection<BsonDocument> GetDataFromMongo(string collection)
        {

            var mongoServer = MongoServer.GetAllServers()
                    .FirstOrDefault(name => name.GetDatabase(dbname)
                    .CollectionExists(dbname));
            var database = mongoServer.GetDatabase(dbname);
            Func<MongoDatabase, string, MongoCollection<BsonDocument>> exist = (db, db_name) =>
            {
                if (db.CollectionExists(db_name))
                    return db.GetCollection(db_name);
                return default(MongoCollection<BsonDocument>);
            };

            return exist(
                database, dbname);
        }

        //Более свободная реализация
        public MongoCollection GetDataFromMongo(string collection, Func<string, bool> predicate)
        {
            if (predicate(collection))
                return GetDataFromMongo(collection);
            return default(MongoCollection);
        }

        public void WriteDatainMongo(MongoCollection coll, WriteInfo info)
        {
            if (coll != null)
                coll.Insert<BsonDocument>(new BsonDocument{
                    
                        {"Author",System.Environment.MachineName},
                        {"Message",info.message},
                        {"category",info.category}
                    });
        }

        //Запись при ошибке
        public void WriteErrorDatainMongo(MongoCollection coll, string error_message)
        {
            coll.Insert<BsonDocument>(new BsonDocument
            {
                {"Author", System.Environment.MachineName},
                {"Error", error_message}
            });
        }
    }
}
