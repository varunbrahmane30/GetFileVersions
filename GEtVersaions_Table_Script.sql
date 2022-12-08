
create table FileVersions (
	Id int Primary key,
	Host varchar(150),
	FileName varchar(200),
	FileVersion varchar(200),
	Path varchar(450)

);

select * from FileVersions