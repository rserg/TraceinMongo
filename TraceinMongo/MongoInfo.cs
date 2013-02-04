using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace TraceinMongo
{
    class MongoInfo
    {
        private string dbname;
        private MongoData data;
        private int servercount;
        public MongoInfo(string dbname, MongoData data)
        {
            this.dbname = dbname;
            this.data = data;
            this.servercount = data.GetServerCount();
        }

        public static MongoInfo Load(string dbname)
        {
            return new MongoInfo(dbname, new MongoData(dbname));
        }
        public MongoCollection<BsonDocument> ReadFromMongo()
        {
            Func<MongoData, Func<string,
                MongoCollection<BsonDocument>>> curss =
                delegate(MongoData data)
                {
                    return delegate(string dbname)
                    {
                        return data.GetDataFromMongo(dbname);
                    };
                };


            return curss(new MongoData(this.dbname))(this.dbname);
        }



        public void Show()
        {
            MongoData data = new MongoData(dbname);
            foreach (var tt in data.GetDataFromMongo(dbname).FindAll())
            {
                Console.WriteLine(tt["Message"].AsString);
            }
        }

        public int ServersCount()
        {
            return this.servercount;
        }

        public IEnumerable<BsonDocument> Find(string key)
        {
            var e = from store in data.GetDataFromMongo(dbname).FindAll()
                    where store["Message"].AsString == key
                    select store;

            return e;

        }

        public void ClearBase(string dbname)
        {
            MongoData data = new MongoData(dbname);
            data.GetDataFromMongo(dbname).RemoveAll();
        }

        public long Size()
        {
            MongoData data = new MongoData(dbname);
            return data.GetDataFromMongo(dbname).Count();
        }

        public void ChangeDBName(string newdbname)
        {
            this.dbname = newdbname;
        }
    }
}
