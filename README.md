This software is used to compare 2 GEDCOM files. Configuration is completed using the appsettings.json file. The configuration file includes the following values
a) LoggingLevel this states the level of information to be provided within the logfile produced. 
b) masterFile this is the file from which the comparison is made. 
c) comparisonFile this file is compared against the masterfile and reports where
  1) The person exists in the masterFile but does not exist within the comparison file
  2) People in the comparison file have not been traversed. This means they are not linked into the tree and as such would never have been matched.
Comparison is completed on Name (Full name) and Date of Birth. Comparison is not case sensitive but does require all names to be included (First, Middle & Surname).
The report will highlight the values being compared when no match is being found.
Dates are compared using long and short form. E.g. 2 Feb 2023 would match 2 February 2023 and 02 Feb 2023 BUT NOT 02/02/2023.

Execution currently requires dotnet 8.0.201
