using System.Diagnostics;
using System.Text;
using System.Text.Json;
using static System.String;

namespace update_modified
{
    internal class Program
    {
        static bool useOPF = false;
        static bool useModified = true;
        static bool reversed = false;
        static string databasePath = "";
        static bool verbose = false;

        static void checkForDatabasePath( string path )
        {
            databasePath = path;
            while (!IsNullOrWhiteSpace(databasePath) && !File.Exists( Path.Combine(databasePath , "metadata.db" )))
            {
                databasePath = Path.GetDirectoryName( databasePath );
            }
            if (IsNullOrWhiteSpace(databasePath ))
            {
                Console.WriteLine( "Unable to derive database path from book format path: "+ path );
                return;
            }
            if (verbose) Console.WriteLine( "Database path: " + databasePath );
        }

        static string getExtension( string path )
        {
            string ext = Path.GetExtension( path );
            if (ext == null) return "";
            return ext.ToLower();
        }

        static Rootobject getSQL()
        {
            Process process = new Process();
            process.StartInfo.FileName = "calibredb";
            if (!IsNullOrWhiteSpace( databasePath ))
            {
                process.StartInfo.Arguments = "--with-library=\"" + databasePath + "\" list -f last_modified,formats --for-machine";
            }
            else
            {
                process.StartInfo.Arguments = "list -f last_modified,formats --for-machine";
            }
            if (verbose) Console.WriteLine( "Executing: calibredb " + process.StartInfo.Arguments );
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                Console.WriteLine( "Error executing calibredb: " + process.ExitCode );
                Environment.Exit( 1 );
            }

            Rootobject jsonData = null;
            try
            {
                jsonData = JsonSerializer.Deserialize<Rootobject>("{ \"books\": "+output+"}");
            }
            catch (Exception e)
            {
                Console.WriteLine( "Error parsing JSON data: " + e.Message );
                Environment.Exit( 1 );
            }
            if (jsonData == null)
            {
                Console.WriteLine( "Unable to parse JSON data from calibredb" );
                Environment.Exit( 1 );
            }
            if (jsonData.books == null || jsonData.books.Length == 0)
            {
                Console.WriteLine( "No books found in database" );
                Environment.Exit( 1 );
            }
            if (verbose) Console.WriteLine( string.Format( "Found {0} books", jsonData.books.Length ) );

            return jsonData;
        }

        static void runSQL(string script)
        {
            Process process = new Process();
            process.StartInfo.FileName = "calibre-debug";
            process.StartInfo.Arguments = "-e " + script;
            if (verbose) Console.WriteLine( "Executing: calibre-debug " + process.StartInfo.Arguments );
            process.StartInfo.UseShellExecute = false;
            process.Start();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                Console.WriteLine( "Error executing python script: " + process.ExitCode );
                Console.WriteLine( "Please check database state." );
                Environment.Exit( 1 );
            }
        }

        static void processDirectory(string dirPath, int id, DateTime modified, List<string> sql)
        {
            DateTime checkDate;
            DateTime startDate;
            DateTime fileDate;
            string[] files = Directory.GetFiles(dirPath, "*.*");

            if (reversed)
            {
                checkDate = DateTime.MaxValue;
            }
            else
            {
                checkDate = DateTime.MinValue;
            }
            startDate = checkDate;

            foreach (string file in files)
            {
                if (useOPF)
                {
                    if (getExtension( file ) != ".opf")
                    {
                        continue;
                    }
                }
                else
                {
                    if (getExtension( file ) == ".opf")
                    {
                        continue;
                    }
                }

                if (useModified)
                {
                    fileDate = File.GetLastWriteTime(file);
                }
                else
                {
                    fileDate = File.GetCreationTime(file);
                }

                if (reversed)
                {
                    if (fileDate < checkDate)
                    {
                        checkDate = fileDate;
                    }
                }
                else
                {
                    if (fileDate > checkDate)
                    {
                        checkDate = fileDate;
                    }
                }
            }

            if (checkDate != startDate)
            {
                // 2024-01-17 12:35:52+00:00
                string date1 = modified.ToString("yyyy-MM-dd HH:mm:ss");
                string date2 = checkDate.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss");
                if (date1 != date2)
                {
                    sql.Add( string.Format( "update books set last_modified = '{0}+00:00' where id={1};", date2, id ) );
                }
            }
        }

        static void usage()
        {
            Console.WriteLine(
"""
Usage: update_modified [options]
Options:
  -h, --help            Show this help message and exit
  --library-path=<path> Specify the path to the calibre library
  --with-library=<path> Specify the path to the calibre library
  -o, --use-opf         Use OPF files for modification date
  -c, --use-create      Use file creation date for modification date
  -m, --use-modified    Use file last modified date for modification date
  -r, --reversed        Reverse the comparison (check if file is older)
  -v, --verbose         Enable verbose output

  As usual, double quotes are required for paths with spaces.
  If the library path is not specified the path in settings will be used.
"""
);
            Environment.Exit( 1 );
        }

        static void Main(string[] args)
        {
            string[] subArgs;
            /*
             * Process parms if any
             */
            if (args.Length > 0)
            {
                int i = 0;
                while (i < args.Length)
                {
                    subArgs = args[i].Split( '=',StringSplitOptions.RemoveEmptyEntries );
                    switch (subArgs[0].ToLower())
                    {
                        case "-h":
                        case "--help":
                        {
                            usage();
                            break;
                        }
                        case "--library-path":
                        case "--with-library":
                        {
                            if (subArgs.Length == 2)
                            {
                                databasePath = subArgs[1];
                                if (databasePath.StartsWith( '~' ))
                                {
                                    databasePath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ), databasePath.Substring( 1 ).TrimStart('\\','/') );
                                }
                                else if (databasePath.StartsWith( '.' ))
                                {
                                    databasePath = Path.Combine( Directory.GetCurrentDirectory(), databasePath.Substring( 1 ) );
                                }
                                else if (databasePath.StartsWith( '/' ))
                                {
                                    databasePath = Path.Combine( Path.DirectorySeparatorChar.ToString(), databasePath.Substring( 1 ) );
                                }
                                if (Path.GetFileName( databasePath ) == "metadata.db")
                                {
                                    databasePath = Path.GetDirectoryName( databasePath );
                                }
                                if (!Directory.Exists( databasePath ) || !File.Exists(Path.Combine(databasePath,"metadata.db")))
                                {
                                    Console.WriteLine( "Invalid database path: " + databasePath );
                                    usage();
                                }
                            }
                            else
                            {
                                Console.WriteLine( "Missing database path" );
                                usage();
                            }
                            break;
                        }
                        case "-o":
                        case "--use-opf":
                        {
                            useOPF = true;
                            break;
                        }
                        case "-c":
                        case "--use-create":
                        {
                            useModified = false;
                            break;
                        }
                        case "-m":
                        case "--use-modified":
                        {
                            useModified = true;
                            break;
                        }
                        case "-r":
                        case "--reversed":
                        {
                            reversed = true;
                            break;
                        }
                        case "-v":
                        case "--verbose":
                        {
                            verbose = true;
                            break;
                        }
                        default:
                        {
                            usage();
                            break;
                        }
                    }
                    i++;
                }
            }

            JSONData[] books = getSQL().books;
            List<string> sql = new List<string>();

            foreach (JSONData book in books)
            {
                int id = book.id;

                DateTime modified = book.last_modified.ToUniversalTime();

                if (book.formats.Length == 0)
                {
                    if (verbose) Console.WriteLine( "No book paths found for id: " + id );
                    continue;
                }

                bool found = false;
                foreach(string bPath in book.formats)
                {
                    if (File.Exists( bPath ))
                    {
                        found = true;
                        if (IsNullOrWhiteSpace(databasePath))
                        {
                            checkForDatabasePath(Path.GetDirectoryName( bPath ));
                        }
                        processDirectory( Path.GetDirectoryName( bPath ), id, modified, sql );
                        break;
                    }
                }

                if (!found)
                {
                    if (verbose) Console.WriteLine( "No valid book path found for id: " + id );
                    continue;
                }
            }

            if (sql.Count == 0)
            {
                if (verbose) Console.WriteLine( "No updates found." );
                return;
            }

            if (verbose) Console.WriteLine( string.Format( "Found {0} updates", sql.Count ) );
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("from calibre.db.backend import DB;");
            sb.AppendFormat("db = DB('{0}');",databasePath.Replace('\\','/'));
            sb.AppendLine();
            int j = 0;
            foreach ( string line in sql)
            {
                sb.AppendFormat("db.execute(\"{0}\");",line);
                sb.AppendLine();
                j++;
                if (verbose) sb.AppendLine( string.Format( @"print(""Book: {0}"",end='\r');", j ) );
            }
            string pyScript = Path.Combine( Path.GetTempPath(), "update_sql_LM.py" );
            File.WriteAllText( pyScript, sb.ToString() );
            runSQL( pyScript );
            if (verbose) Console.WriteLine( string.Format( "Updated {0} books", sql.Count ) );
            File.Delete( pyScript );
        }
    }
}
