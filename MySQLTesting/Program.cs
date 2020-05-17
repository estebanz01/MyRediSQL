using CachingFramework.Redis;
using Microsoft.EntityFrameworkCore;
using MySQLTesting.Employees;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MySQLTesting
{
    class salaryGroupData
    {
        public int EmployeeId { get; set; }
        public Double SalaryCalculation { get; set; }
    }

    class SalaryTwo
    {
        public int employeeId { get; set; }
        public Double SalarySum { get; set; }
    }

    class Program
    {
        // This is the line that connects to Redis
        // You can specify the server as a connection string in the
        // new RedisContext("connection-string-here");
        public static RedisContext redis = new RedisContext();

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            Stopwatch stopWatchTotal = Stopwatch.StartNew();
            Stopwatch stopWatchRedis = Stopwatch.StartNew();

            string redisKey = "employeesResult";

            //ReadData();
            List<salaryGroupData> data = redis.Cache.FetchObject<List<salaryGroupData>>(
                redisKey,
                () => CommonSQLWhere(),
                TimeSpan.FromMinutes(3)
                );

            var res = TransformData(data);

            stopWatchRedis.Stop();

            foreach (var message in from result in res.OrderByDescending((x) => x.SalarySum)
                                    let message = new StringBuilder()
                      .AppendLine($"EmployeeID: {result.employeeId}, AvgSalary: {result.SalarySum}")
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

        private static List<salaryGroupData> CommonSQLWhere()
        {
            using (var context = new Employees.employeesContext())
            {
                DateTime dateTime = new DateTime(2001, 01, 01);
                var orderedQueryable = from e in context.Employees
                                       join s in context.Salaries
                                       on e.EmpNo equals s.EmpNo
                                       where s.FromDate >= dateTime
                                       select new salaryGroupData { EmployeeId = e.EmpNo, SalaryCalculation = s.Salary };

                return orderedQueryable.ToList();
            }
        }

        private static List<SalaryTwo> TransformData(List<salaryGroupData> l)
        {
            return l.GroupBy(x => x.EmployeeId).Select(y => new SalaryTwo { employeeId = y.Key, SalarySum = y.Sum(x => x.SalaryCalculation) }).ToList();
        }
    }
}
