﻿using ConsoleTableExt;
using FlashCards.Model;
using System.Data.SqlClient;
using System.Diagnostics;

namespace FlashCards
{
	internal class DataAccess
	{
		internal static void SetupDbAndTables(string connectionString)
		{
			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				var sqlCommand = connection.CreateCommand();
				sqlCommand.CommandText = "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Flashcards') CREATE DATABASE Flashcards";
				sqlCommand.ExecuteNonQuery();
				sqlCommand.CommandText = @"IF NOT EXISTS ( SELECT * FROM sysobjects WHERE name = 'Stack' and type = 'U')
				CREATE TABLE Stack (StackId int NOT NULL PRIMARY KEY IDENTITY(1,1),
								     StackName nvarchar(255) NOT NULL UNIQUE)";
				sqlCommand.ExecuteNonQuery();
				sqlCommand.CommandText = @"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'Flashcard' and type ='U')
				CREATE TABLE Flashcard (FlashcardId int PRIMARY KEY NOT NULL IDENTITY(1,1),
										FrontText nvarchar(255) NOT NULL,
										BackText nvarchar(255) NOT NULL,
										StackId int NOT NULL,
										CONSTRAINT FK_StackFlashcard FOREIGN KEY (StackId)
										REFERENCES Stack(StackId)
										ON DELETE CASCADE
										ON UPDATE CASCADE)";
				sqlCommand.ExecuteNonQuery();
				sqlCommand.CommandText = @"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = 'StudySession' and type ='U')
				CREATE TABLE StudySession (StudySessionId int PRIMARY KEY NOT NULL IDENTITY(1,1),
											StackId int NOT NULL,
											StackName nvarchar(255) NOT NULL,
											StudyDate date NOT NULL DEFAULT getdate(),
											Score int NOT NULL
											CONSTRAINT FK_StackStudySession FOREIGN KEY (StackId)
											REFERENCES Stack(StackId)
											ON DELETE CASCADE
											ON UPDATE CASCADE)";
				sqlCommand.ExecuteNonQuery();
				connection.Close();
			}
		}
		internal static void Stack(string connectionString)
		{
			UserInput.GetStackMenuInput(connectionString);
		}
		internal static void Flashcards(string connectionString)
		{
			Console.Clear();
			var stackInfo = UserInput.GetStackName(connectionString);
			UserInput.GetFlashCardMenuInput(connectionString, stackInfo.stackName, stackInfo.stackId);
		}
		internal static void Study(string connectionString)
		{
			Console.Clear();
			var stackInfo = UserInput.GetStackName(connectionString);
			UserInput.GetStudyMenuInput(connectionString, stackInfo.stackName, stackInfo.stackId);
		}
		internal static void ShowStackNames(string connectionString)
		{
			List<StackNameDTO> stackNames = BuildStackDTO(connectionString);
			ConsoleTableBuilder
					.From(stackNames)
					.WithColumn("List of Stack Names")
					.ExportAndWriteLine();
		}
		internal static List<Stack> BuildStack(string connectionString, string command)
		{
			List<Stack> stack = new();
			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				var sqlCommand = connection.CreateCommand();
				sqlCommand.CommandText = command;
				SqlDataReader reader = sqlCommand.ExecuteReader();
				if (reader.HasRows)
				{
					while (reader.Read())
					{
						stack.Add(new Stack
						{
							StackId = reader.GetInt32(0),
							StackName = reader.GetString(1)
						});
					}
				}
			}
			return stack;
		}
		internal static List<StackNameDTO> BuildStackDTO(string connectionString)
		{
			List<StackNameDTO> stackNames = new();
			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				var sqlCommand = connection.CreateCommand();
				sqlCommand.CommandText = "SELECT StackName FROM dbo.Stack";

				SqlDataReader reader = sqlCommand.ExecuteReader();
				if (reader.HasRows)
				{
					stackNames.Clear();
					while (reader.Read())
					{
						stackNames.Add(new StackNameDTO
						{
							StackName = reader.GetString(0)
						});
					}
				}
				else
					Console.WriteLine("No records found");
				connection.Close();
			}
			return stackNames;
		}
		internal static void CreateNewStack(string connectionString)
		{
			Console.Clear();
			string stackName = UserInput.NewStack(connectionString);
			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				var sqlCommand = connection.CreateCommand();
				sqlCommand.CommandText = "INSERT INTO dbo.Stack (StackName) VALUES (@stackName)";
				sqlCommand.Parameters.Add(new SqlParameter("@stackName", stackName));
				sqlCommand.ExecuteNonQuery();
				connection.Close();
			}
			Console.WriteLine($"New stack {stackName} created");
			UserInput.GetStackMenuInput(connectionString);
		}
		internal static bool DoesStackExist(string connectionString, string inputName)
		{
			bool doesExist = false;
			List<StackNameDTO> existingStacks = new List<StackNameDTO>();
			existingStacks = BuildStackDTO(connectionString);
			foreach (var stack in existingStacks)
			{
				if (inputName.ToLower() == stack.StackName.ToLower())
				{
					doesExist = true;
				}
			}
			return doesExist;
		}
		internal static void DeleteStack(string connectionString)
		{
			Console.Clear();
			string command = "SELECT * from dbo.Stack";
			List<Stack> stack = BuildStack(connectionString, command);
			ConsoleTableBuilder
				.From(stack)
				.WithTitle("Stacks")
				.ExportAndWriteLine();
			string stackId = UserInput.DeleteStack(connectionString, stack);
			if (stackId == "0")
			{
				Stack(connectionString);
			}
			else
			{
				using (var connection = new SqlConnection(connectionString))
				{
					connection.Open();
					var sqlCommand = connection.CreateCommand();
					sqlCommand.CommandText = "DELETE FROM dbo.Stack WHERE StackId = (@stackId)";
					sqlCommand.Parameters.Add(new SqlParameter("@stackId", stackId));
					sqlCommand.ExecuteNonQuery();
					connection.Close();
				}
				Console.WriteLine("Stack succesfully deleted");
			}
            UserInput.GetStackMenuInput(connectionString);
        }
		internal static void RenameStack(string connectionString)
		{
			Console.Clear();
			string command = "SELECT * from dbo.Stack";
			List<Stack> stack = BuildStack(connectionString, command);
			ConsoleTableBuilder
				.From(stack)
				.WithTitle("Stacks")
				.ExportAndWriteLine();
			var newStackInfo = UserInput.RenameStack(connectionString, stack);
			if (newStackInfo.idToRename == "0")
			{
				Stack(connectionString);
			}
			else
			{
				using (var connection = new SqlConnection(connectionString))
				{
					connection.Open();
					var sqlCommand = connection.CreateCommand();
					sqlCommand.CommandText = "UPDATE dbo.Stack SET StackName = (@stackName) WHERE StackId = (@stackId)";
					sqlCommand.Parameters.Add(new SqlParameter("@stackId", newStackInfo.idToRename));
					sqlCommand.Parameters.Add(new SqlParameter("@stackName", newStackInfo.newName));
					sqlCommand.ExecuteNonQuery();
					connection.Close();
				}
				Console.WriteLine("Stack succesfully renamed");
			}
            UserInput.GetStackMenuInput(connectionString);
        }
		internal static List<FlashcardDTO> BuildFlashcardDTOcustomId(string connectionString, string stackId)
		{
			List<FlashcardDTO> flashcards = new();
			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				var sqlCommand = connection.CreateCommand();
				sqlCommand.CommandText = "SELECT FrontText, BackText from dbo.Flashcard WHERE StackId = @stackId";
				sqlCommand.Parameters.Add(new SqlParameter("@stackId", stackId));
				SqlDataReader reader = sqlCommand.ExecuteReader();
				if (reader.HasRows)
				{
					int id = 1;
					while (reader.Read())
					{
						flashcards.Add(new FlashcardDTO
						{
							Id = id,
							FrontText = reader.GetString(0),
							BackText = reader.GetString(1)
						});
						id++;
					}
				}
			}
			return flashcards;
		}
		internal static List<FlashcardDTO> BuildFlashcardDTO(string connectionString, string stackId)
		{
			List<FlashcardDTO> flashcards = new();
			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				var sqlCommand = connection.CreateCommand();
				sqlCommand.CommandText = "SELECT FlashcardId, FrontText, BackText from dbo.Flashcard WHERE StackId = @stackId";
				sqlCommand.Parameters.Add(new SqlParameter("@stackId", stackId));
				SqlDataReader reader = sqlCommand.ExecuteReader();
				if (reader.HasRows)
				{
					while (reader.Read())
					{
						flashcards.Add(new FlashcardDTO
						{
							Id = reader.GetInt32(0),
							FrontText = reader.GetString(1),
							BackText = reader.GetString(2)
						});
					}
				}
			}
			return flashcards;
		}
		internal static void ShowAllFlashcards(string connectionString, string stackName, string stackId)
		{
			Console.Clear();
			List<FlashcardDTO> flashcards = BuildFlashcardDTOcustomId(connectionString, stackId);
			if (flashcards.Count == 0)
				Console.WriteLine("No flashcards found for this stack");
			else
			{
				ConsoleTableBuilder
					.From(flashcards)
					.WithTitle(stackName)
					.WithColumn("Id", "Front", "Back")
					.ExportAndWriteLine();
			}
			Console.ReadLine();
            UserInput.GetFlashCardMenuInput(connectionString, stackName, stackId);
        }
		internal static void ShowXFlashcards(string connectionString, string stackName, string stackId)
		{
			Console.Clear();
			List<FlashcardDTO> flashcards = BuildFlashcardDTOcustomId(connectionString, stackId);
			if (flashcards.Count == 0)
				Console.WriteLine($"The stack {stackName} doesn't contain any flashcards");
			else
			{
				Console.WriteLine($"The stack {stackName} contains {flashcards.Count} flashcards");
				bool validInt = false;
				while (validInt == false)
				{
					Console.WriteLine("How many would you like to display ?");
					string input = Console.ReadLine();
					if (Helpers.IsValidInt(input))
					{
						int number = int.Parse(input);
						List<FlashcardDTO> xFlashcards = new();
						using (var connection = new SqlConnection(connectionString))
						{
							connection.Open();
							var sqlcommand = connection.CreateCommand();
							sqlcommand.CommandText = "SELECT TOP (@number) FrontText, BackText from dbo.Flashcard WHERE StackId = @stackId";
							sqlcommand.Parameters.Add(new SqlParameter("@number", number));
							sqlcommand.Parameters.Add(new SqlParameter("@stackId", stackId));
							SqlDataReader reader = sqlcommand.ExecuteReader();
							if (reader.HasRows)
							{
								int id = 1;
								while (reader.Read())
								{
									xFlashcards.Add(new FlashcardDTO
									{
										Id = id,
										FrontText = reader.GetString(0),
										BackText = reader.GetString(1)
									});
									id++;
								}
							}
						}
						ConsoleTableBuilder
							.From(xFlashcards)
							.WithTitle(stackName)
							.WithColumn("Id", "Front", "Back")
							.ExportAndWriteLine();
						validInt = true;
					}
					else
					{
						Console.WriteLine("Not a valid number try again");
					}
				}
			}
            Console.ReadLine();
            UserInput.GetFlashCardMenuInput(connectionString, stackName, stackId);
        }
		internal static void CreateFlashcard(string connectionString, string stackName, string stackId)
		{
			Console.Clear();
			Console.WriteLine("Create new flashcard:");
			Console.WriteLine("---------------------");
			string frontText = UserInput.GetFlashCardFront();
			string backText = UserInput.GetFlashCardBack();
			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				var sqlCommand = connection.CreateCommand();
				sqlCommand.CommandText = "INSERT INTO dbo.FlashCard (FrontText, BackText, StackId) VALUES (@frontText, @backText, @stackId)";
				sqlCommand.Parameters.Add(new SqlParameter("@frontText", frontText));
				sqlCommand.Parameters.Add(new SqlParameter("@backText", backText));
				sqlCommand.Parameters.Add(new SqlParameter("@stackId", stackId));
				sqlCommand.ExecuteNonQuery();
				connection.Close();
			}
			UserInput.GetFlashCardMenuInput(connectionString, stackName, stackId);

        }
		internal static void ModifyFlashcard(string connectionString, string stackName, string stackId)
		{
			Console.Clear();
			List<FlashcardDTO> flashcards = BuildFlashcardDTO(connectionString, stackId);
			ConsoleTableBuilder
				.From(flashcards)
				.WithTitle("Flashcards")
				.WithColumn("Id", "Front", "Back")
				.ExportAndWriteLine();
			bool validId = false;
			while (validId == false)
			{
				Console.WriteLine("Enter the id of the card you want to edit or 0 to return: ");
				string flashcardId = Console.ReadLine();
				if (flashcardId == "0")
					break;
				else if (Helpers.ValidateId(flashcardId) && Helpers.DoesFlashcardIdExists(flashcardId, flashcards))
				{
					validId = true;
					string frontText = UserInput.GetFlashCardFront();
					string backText = UserInput.GetFlashCardBack();
					using (var connection = new SqlConnection(connectionString))
					{
						connection.Open();
						var sqlCommand = connection.CreateCommand();
						sqlCommand.CommandText = "UPDATE dbo.FlashCard SET FrontText = @frontText, BackText = @backText WHERE FlashcardId = (@flashcardId)";
						sqlCommand.Parameters.Add(new SqlParameter("@frontText", frontText));
						sqlCommand.Parameters.Add(new SqlParameter("@backText", backText));
						sqlCommand.Parameters.Add(new SqlParameter("@flashcardId", flashcardId));
						sqlCommand.ExecuteNonQuery();
						connection.Close();
					}
				}
				else
					Console.WriteLine("A flashcard with this id does not exist, try again");
			}
            UserInput.GetFlashCardMenuInput(connectionString, stackName, stackId);
        }
		internal static void DeleteFlashcard(string connectionString, string stackName, string stackId)
		{
			Console.Clear();
			List<FlashcardDTO> flashcards = BuildFlashcardDTO(connectionString, stackId);
			ConsoleTableBuilder
				.From(flashcards)
				.WithTitle("Flashcards")
				.WithColumn("Id", "Front", "Back")
				.ExportAndWriteLine();
			bool validId = false;
			while (validId == false)
			{
				Console.WriteLine("Enter the id of the card you want to delete or 0 to return: ");
				string flashcardId = Console.ReadLine();
				if (flashcardId == "0")
					break;
				else if (Helpers.ValidateId(flashcardId) && Helpers.DoesFlashcardIdExists(flashcardId, flashcards))
				{
					validId = true;
					using (var connection = new SqlConnection(connectionString))
					{
						connection.Open();
						var sqlCommand = connection.CreateCommand();
						sqlCommand.CommandText = "DELETE FROM dbo.Flashcard WHERE FlashcardId = (@flashcardId)";
						sqlCommand.Parameters.Add(new SqlParameter("@flashcardId", flashcardId));
						sqlCommand.ExecuteNonQuery();
						connection.Close();
					}
				}
				else
					Console.WriteLine("A flashcard with that id does not exist, please try again");
			}
            UserInput.GetFlashCardMenuInput(connectionString, stackName, stackId);
        }
		internal static void TakeQuiz(string connectionString, string stackName, string stackId)
		{
			Console.Clear();
			List<FlashcardDTO> flashcardList = BuildFlashcardDTO(connectionString, stackId);
			int score = 0;
			foreach (var item in flashcardList)
			{
				List<StudyFrontDTO> studyList = new();
				studyList.Add(new StudyFrontDTO
				{
					Front = item.FrontText
				});
				ConsoleTableBuilder
					.From(studyList)
					.WithTitle(stackName)
					.WithColumn("Front")
					.ExportAndWriteLine();

				string input = UserInput.GetStudyAnswer();
				string answer = item.BackText;

				if (input == "0")
				{
					UserInput.GetMenuInput(connectionString);
					break;
				}
				else if (input.ToLower() == answer.ToLower())
				{
					Console.WriteLine("Your answer is correct !!");
					score++;
				}
				else
				{
					Console.WriteLine("Your answer was wrong.");
					Console.WriteLine($"The correct answer was {answer}");
				}
				Console.ReadKey();
				Console.Clear();
			}

			CreateStudySession(connectionString, stackId, stackName, score);
			Console.WriteLine("Exiting Study session");
			Console.WriteLine($"You got {score} right out of {flashcardList.Count}");
			Console.ReadKey();
		}
		private static void CreateStudySession(string connectionString, string stackId, string stackName, int score)
		{
			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				var sqlCommand = connection.CreateCommand();
				sqlCommand.CommandText = "INSERT INTO dbo.StudySession (StackId, StackName, Score) VALUES (@stackId, @stackName, @score)";
				sqlCommand.Parameters.Add(new SqlParameter("@stackId", stackId));
				sqlCommand.Parameters.Add(new SqlParameter("@stackName", stackName));
				sqlCommand.Parameters.Add(new SqlParameter("@score", score));
				sqlCommand.ExecuteNonQuery();
				connection.Close();
			}
		}
		internal static void ViewStudyData(string connectionString)
		{
			Console.Clear();
			List<StudySession> sessions = new();
			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				var sqlCommand = connection.CreateCommand();
				sqlCommand.CommandText = "SELECT StudySessionId, StackId, StackName, StudyDate, Score FROM dbo.StudySession";
				SqlDataReader reader = sqlCommand.ExecuteReader();
				if (reader.HasRows)
				{
					while (reader.Read())
					{
						sessions.Add(new StudySession
						{
							StudySessionId = reader.GetInt32(0),
							StackId = reader.GetInt32(1),
							StackName = reader.GetString(2),
							StudyDate = reader.GetDateTime(3),
							Score = reader.GetInt32(4),
						});
					}
				}
			}
			ConsoleTableBuilder
				.From(sessions)
				.WithTitle("Study Sessions")
				.WithColumn("Id", "StackId", "StackName", "Date", "Score")
				.WithFormatter(3, f => $"{f:dd/MM/yy}")
				.ExportAndWriteLine();
			Console.ReadKey();
		}
		internal static void ReportNumberOfSessions(string connectionString)
		{
			Console.Clear();
			string year = UserInput.GetYearForPivot();
			List<ReportModel> numberOfSessions = new();
			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				var sqlCommand = connection.CreateCommand();
				sqlCommand.CommandText = @"SELECT stackname, [January], [February], [March], [April], [May], [June], [July], [August], [September], [October], [November], [December]
											FROM
											(
												SELECT Stack.StackName, DATENAME(MONTH, StudySession.StudyDate) AS StudyMonth
												FROM Stack
												INNER JOIN StudySession ON Stack.StackId = StudySession.StackId
												WHERE YEAR(StudySession.StudyDate) = @year
											) AS s
											PIVOT
											(
												COUNT(StudyMonth)
												FOR StudyMonth IN ([January], [February], [March], [April], [May], [June], [July], [August], [September], [October], [November], [December])
											) AS pvt;
											";
				sqlCommand.Parameters.Add(new SqlParameter("@year", year));
				sqlCommand.ExecuteNonQuery();
				SqlDataReader reader = sqlCommand.ExecuteReader();
				if (reader.HasRows)
				{
					while (reader.Read())
					{
						numberOfSessions.Add(new ReportModel
						{
							StackName = reader.GetString(0),
							January = reader.GetInt32(1),
							February = reader.GetInt32(2),
							March = reader.GetInt32(3),
							April = reader.GetInt32(4),
							May = reader.GetInt32(5),
							June = reader.GetInt32(6),
							July = reader.GetInt32(7),
							August = reader.GetInt32(8),
							September = reader.GetInt32(9),
							October = reader.GetInt32(10),
							November = reader.GetInt32(11),
							December = reader.GetInt32(12),
						});
					}
				}
				else
					Console.WriteLine("No records found for this year");
				connection.Close();
			}
			ConsoleTableBuilder
				.From(numberOfSessions)
				.WithTitle("Number of Session per month for: " + year)
				.ExportAndWriteLine();
			Console.ReadKey();
		}
		internal static void ReportAverageScore(string connectionString)
		{
			Console.Clear();
			string year = UserInput.GetYearForPivot();
			List<ReportModel> averageScore = new();
			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				var sqlCommand = connection.CreateCommand();
				sqlCommand.CommandText = @"SELECT stackname, [January], [February], [March], [April], [May], [June], [July], [August], [September], [October], [November], [December]
										FROM (SELECT Stack.StackName, DATENAME(MONTH, StudySession.StudyDate) AS StudyMonth, StudySession.Score As score
										FROM Stack
										INNER JOIN StudySession ON Stack.StackId = StudySession.StackId
										WHERE YEAR(StudySession.StudyDate) = @year) AS s
										PIVOT (AVG(score) FOR StudyMonth 
										IN ([January], [February], [March], [April], [May], [June], [July], [August], [September], [October], [November], [December])) AS pvt;";
				sqlCommand.Parameters.Add(new SqlParameter("@year", year));
				sqlCommand.ExecuteNonQuery();
				SqlDataReader reader = sqlCommand.ExecuteReader();
				if (reader.HasRows)
				{
					while (reader.Read())
					{
						averageScore.Add(new ReportModel
						{
							StackName = reader.GetString(0),
							January = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
							February = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
							March = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
							April = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
							May = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
							June = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
							July = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
							August = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
							September = reader.IsDBNull(9) ? 0 : reader.GetInt32(9),
							October = reader.IsDBNull(10) ? 0 : reader.GetInt32(10),
							November = reader.IsDBNull(11) ? 0 : reader.GetInt32(11),
							December = reader.IsDBNull(12) ? 0 : reader.GetInt32(12)
						});
					}
				}
				else
                    Console.WriteLine("No records found for that year");
            }
			ConsoleTableBuilder
				.From(averageScore)
				.WithTitle("Average score per month for: " + year)
				.ExportAndWriteLine();
			Console.ReadKey();
		}
	}
}
