type
  actor = record
    age: integer;
    salary: float; 
    children: Array[-1..10] of Integer; 
  end;

  film = record
    actors : Array[1..20] of actor;
  end;
                          

var age: integer;
    salary : float;
    children :  Array[-1..10] of Integer;
    actors : Array[1..20] of actor;
    greenmile : film;
    
begin
    actors := greenmile.actors;
    greenmile.actors := actors;
    
    age := greenmile.actors[2].age;;
    greenmile.actors[2].age := age;
    
    salary := greenmile.actors[2].age;;
    greenmile.actors[2].salary := salary;
    
end.
