using System;
using ProtoBuf;

namespace RedisExample.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var samplePoco1 = new SamplePoco
                {
                    Id = 1,
                    Name = "Michael",
                    Age = 23,
                    Sex = SexEnum.No
                };

                var samplePoco2 = new SamplePoco
                {
                    Id = 2,
                    Name = "Washington",
                    Age = 38,
                    Sex = SexEnum.Yes
                };
                System.Console.WriteLine("2 Poco instances created");

                System.Console.WriteLine("{0} is {1} years old, Id:{2}, Sex:{3}",samplePoco1.Name, samplePoco1.Age,
                    samplePoco1.Id,samplePoco1.Sex);
                System.Console.WriteLine("{0} is {1} years old, Id:{2}, Sex:{3}", samplePoco2.Name, samplePoco2.Age,
                    samplePoco2.Id, samplePoco2.Sex);

                // save first poco to Redis
                var sample1Key = RedisKeys.GetSampleKey(samplePoco1.Id);
                RedisClient.RedisSet(sample1Key,samplePoco1);

                // save second poco to Redis
                var sample2Key = RedisKeys.GetSampleKey(samplePoco2.Id);
                RedisClient.RedisSet(sample2Key, samplePoco2);

                // no funny business 
                samplePoco1 = null;
                samplePoco2 = null;

                var samplePocoRedis1 = RedisClient.RedisGet<SamplePoco>(sample1Key);
                var samplePocoRedis2 = RedisClient.RedisGet<SamplePoco>(sample2Key);

                System.Console.WriteLine("From Redis...");

                System.Console.WriteLine("{0} is {1} years old, Id:{2}, Sex:{3}", samplePocoRedis1.Name, samplePocoRedis1.Age,
                    samplePocoRedis1.Id, samplePocoRedis1.Sex);
                System.Console.WriteLine("{0} is {1} years old, Id:{2}, Sex:{3}", samplePocoRedis2.Name, samplePocoRedis2.Age,
                    samplePocoRedis2.Id, samplePocoRedis2.Sex);

            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Error:", ex);
            }
            finally
            {
                System.Console.WriteLine("End...");
                System.Console.ReadKey();
            }
            

            
        }
    }

    [ProtoContract]
    class SamplePoco
    {
        [ProtoMember(1)]
        public int Id { get; set; }
        [ProtoMember(2)]
        public string Name { get; set; }
        [ProtoMember(3)]
        public int Age { get; set; }
        [ProtoMember(4)]
        public SexEnum Sex { get; set; }
    }
    
    enum SexEnum
    {
        Yes,
        No
    }
}
