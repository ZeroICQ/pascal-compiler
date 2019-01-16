function foo(a: array of integer) : integer;
var 
    i: integer;
begin
    for i := low(a) to high(a) do
        write(a[i], ' ');
        
    for i := low(a) to high(a) do
        a[i] := i;
        
    writeln;
    for i := low(a) to high(a) do
            write(a[i], ' ');
end;


var 
    b : array[10..20] of integer; 
    i : integer;
    
begin
    for i := low(b) to high(b) do
       b[i] := -i;
       
   for i := low(b) to high(b) do
       write(b[i], ' ');
       
    writeln;
        
    foo(b);
    writeln;
    
for i := low(b) to high(b) do
        write(b[i], ' ');
    
end.