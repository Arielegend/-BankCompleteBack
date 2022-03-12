# -BankCompleteBack

At shell 1- 
<p> 
  <li>dotnet build </li>
  <li>dotnet ef migrations add InitialCreate (Use dotnet tool install --global dotnet-ef in case of error) </li>
  <li>docker-compose up</li>
</p>

At shell 2-
<p>
  dotnet ef database update
</p>

Finally, Open VS 2022, and run 'back' profile
