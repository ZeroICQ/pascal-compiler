procedure cast(var d: integer);
begin
    d +=1;
end;


procedure malabar(var d,g : char);
begin
    d := 'z';
    g := 'l'
end;



var i : Integer = 10;
var k,l: char;
begin
    writeln(i);
    cast(i);
    writeln(i);
    l := 'x';
    k := 'm';
    
    malabar(l,k);
    
    writeln(l, ' ', k);
end.
