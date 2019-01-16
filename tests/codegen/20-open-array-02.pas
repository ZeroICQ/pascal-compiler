function foo(var a: array of integer) : integer;
var 
    i: integer;
begin
    for i := low(a) to high(a) do
        write(a[i], ' ');
        
    for i := low(a) to high(a) do
        a[i] := i;
end;


var 
    b : array[10..20] of integer; 
    i : integer;
    
begin
    for i := low(b) to high(b) do
        b[i] := -i;
    
    foo(b);
    
    for i := low(b) to high(b) do
        write(b[i], ' ');
end.