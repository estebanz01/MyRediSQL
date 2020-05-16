using CachingFramework.Redis;
using Microsoft.EntityFrameworkCore;
using MySQLTesting.Employees;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MySQLTesting
{
    class salaryGroupData
    {
        public int EmployeeId { get; set; }
        public Double AvgSalary { get; set; }
    }

    class Program
    {
        public static RedisContext redis = new RedisContext();

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            Stopwatch stopWatchTotal = Stopwatch.StartNew();
            Stopwatch stopWatchRedis = Stopwatch.StartNew();

            string redisKey = "employeesResult";

            //ReadData();
            var data = redis.Cache.FetchObject<List<salaryGroupData>>(
                redisKey,
                () => ReadDataFromDB(),
                TimeSpan.FromMinutes(3)
                );

            stopWatchRedis.Stop();

            foreach (var message in from result in data.OrderByDescending((x) => x.AvgSalary)
                                    let message = new StringBuilder()
                      .AppendLine($"EmployeeID: {result.EmployeeId}, AvgSalary: {result.AvgSalary}")
                                    select message)
            {
                Console.WriteLine(message.ToString());
            }

            stopWatchTotal.Stop();
            
            Console.WriteLine($"Tiempo total transcurrido: {stopWatchTotal.ElapsedMilliseconds / 1000.0}");
            Console.WriteLine($"Tiempo yendo a caché: {stopWatchRedis.ElapsedMilliseconds / 1000.0}");
        }

        private static void ReadData()
        {

            using (var context = new Employees.employeesContext())
            {
                DateTime dateTime = new DateTime(2001, 01, 01);
                var orderedQueryable = from e in context.Employees
                                       join s in context.Salaries
                                       on e.EmpNo equals s.EmpNo
                                       where s.FromDate >= dateTime
                                       group s by e.EmpNo into salaryGroup
                                       select new
                                       {
                                           EmployeeId = salaryGroup.Key,
                                           AvgSalary = salaryGroup.Average(x => x.Salary)
                                       };

                foreach (var message in from result in orderedQueryable.OrderByDescending((x) => x.AvgSalary)
                                        let message = new StringBuilder()
                          .AppendLine($"EmployeeID: {result.EmployeeId}, AvgSalary: {result.AvgSalary}")
                                        select message)
                {
                    Console.WriteLine(message.ToString());
                }
            }
        }

        private static List<salaryGroupData> ReadDataFromDB()
        {
            using (var context = new Employees.employeesContext())
            {
                DateTime dateTime = new DateTime(2001, 01, 01);
                var orderedQueryable = from e in context.Employees
                                       join s in context.Salaries
                                       on e.EmpNo equals s.EmpNo
                                       where s.FromDate >= dateTime
                                       group s by e.EmpNo into sGroup
                                       select new salaryGroupData
                                       {
                                           EmployeeId = sGroup.Key,
                                           AvgSalary = sGroup.Average(x => x.Salary)
                                       };

                return orderedQueryable.ToList();
            }
        }
    }
}
