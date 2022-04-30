using Microsoft.Spark.Sql;
using Sudoku.Shared;
using System;
using System.Diagnostics;
using System.IO;

namespace Sudoku.Z3solver.Spark
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Select Mode: 1. single node 2.cluster");
            var mode = Console.ReadLine();
            Console.WriteLine("Nombre de lignes:");
            var nbrLines = Console.ReadLine();
            //variable pour choisir le mode avec une machine ou plusieurs machine
            int.TryParse(nbrLines, out var intNbrLines);
            //variable pour choisir le mode avec une machine ou plusieurs machine
            int.TryParse(mode, out var intMode);

            SparkSession spark;
            switch (intMode)
            {
                case 1:
                    spark = SparkSession.Builder()
                        .AppName("Z3 Solver Spark")
                        .Config("spark.executor.cores", 1)
                        .Config("spark.executor.instances", 1) // nombre de workers
                        .GetOrCreate();
                    break;
                case 2:
                    spark = SparkSession.Builder()
                        .AppName("Z3 Solver Spark")
                        .Config("spark.executor.cores", 1)
                        .Config("spark.executor.instances", 4) // nombre de workers
                        .GetOrCreate();
                    break;
                default:
                    return;
            }

            var filePath = Path.Combine( Environment.CurrentDirectory,"sudoku.csv");

            DataFrame dataFrame = spark.Read()
                .Option("Reader", true)
                //.Option("inferSchema", true)
                .Schema("quizzes string, solutions string")
                .Csv(filePath);

            DataFrame limitedSudoku = dataFrame.Limit(intNbrLines);

            spark.Udf().Register<string, string>("SudokuZ3Udf", (sudoku) => solveSudoku(sudoku));

            limitedSudoku.CreateOrReplaceTempView("Resolved");
            DataFrame sqlDataframe = spark.Sql("Select quizzes, SudokuZ3Udf(quizzes) as Resolution from Resolved");
            sqlDataframe.Show();

            var stopwatch = Stopwatch.StartNew();

            
            //temps ecoule
            var elapsedTime = stopwatch.Elapsed;

            Console.WriteLine($"Temps d'execution :{ elapsedTime.ToString() }");
        }

        public static string solveSudoku(string sudoku)
        {
            var solver = new Z3AsumptionsSolverSpark();
            var sudokuGrid = SudokuGrid.Parse(sudoku);
            var solvedSudokuGrid = solver.Solve(sudokuGrid);
            return solvedSudokuGrid.ToString();
        }
    }
}
