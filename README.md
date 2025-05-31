# update_modified

This application will update the 'last_modified' date of a book in  `Calibre`.

```bash
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
  If the library path is not specified the path in Calibre settings will be used.
  This is usually the last library openned in Calibre.
```

Please note: This application requires the use ot the `calibre` command line tools,
which are installed with Calibre. Make sure that the `calibre` command is available
in your PATH.

Do not run this application while `calibre` is running unless you specify a library
that is not open in `calibre`.

## Usage example

`update_modified -v` will update the last modified date of all books in the default
Calibre library. The last_modified field for the book will be set to the youngest file
modified date in the directory containing the book except for the .opf file. The `-v`
option causes the application to print update messages about it's actions to the console.

## OS Options

update_modified is written in .Net 9.0. It has been tested on Windows 10 and should
also work on Linux with .Net installed.
