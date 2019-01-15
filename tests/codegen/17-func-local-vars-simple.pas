var 
    i: integer = 500;
    d: double = 20.32;
    c: char = 's';


procedure foo;
var 
    i: integer = 10;
    d: double = -220.2;
    c: char = 'x';
begin
    writeln(i);
    writeln(d);
    writeln(c);
    
    i := 22;
    d := 22.3;
    c := 'o';
    
    writeln(i);
    writeln(d);
    writeln(c);
end;


begin
    writeln(i);
    writeln(d);
    writeln(c);
    
    foo();
    
    writeln(i);
    writeln(d);
    writeln(c);
end.
