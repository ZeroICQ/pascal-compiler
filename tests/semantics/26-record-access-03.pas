type
   cArray = type Array[-1..10] of Integer;

  actor = record
    age: integer;
    salary: double; 
    children: cArray; 
  end;
  
  actorAlias = actor;
  actorTypeAlias = actor;

  film = record
    actors : Array[1..20] of actorTypeAlias;
  end;
                          

var age: integer;
    salary : double;
    children :  Array[-1..10] of Integer;
    actors : Array[1..20] of actorAlias;
    greenmile : film;
    
begin
    actors := greenmile.actors;
    greenmile.actors := actors;
    
    age := greenmile.actors[2].age;;
    greenmile.actors[2].age := age;
    
    salary := greenmile.actors[2].age;;
    greenmile.actors[2].salary := salary;
    
end.
