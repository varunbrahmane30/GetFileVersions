
create table FileVersions (
	Id int Primary key IDENTITY,
	Host varchar(150),
	SoftwareName varchar(200),
	Path varchar(500),
	FileVersion varchar(200), 
	Last_Update datetime,
	Database_Name varchar(250)
);

drop table FileVersions

--insert into AAbFileVersions values ('ad','aa','bb','','')

alter table FileVersions add Last_Update datetime  
alter table FileVersions add Database_Name varchar(250)