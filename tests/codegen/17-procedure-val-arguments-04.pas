type
    arr = array[1..10] of array[1..10] of char;
    
var a, b : arr;

procedure foo(c : arr);
begin
    b := c;
end;

var i, j: integer;

begin
    for i:= 1 to 10 do
        for j := 1 to 10 do 
        begin
            if (i+j mod 2 = 1) then
                a[i][j] := 'a'
            else 
                a[i][j] := 'z';
        end;
                
            
    foo(a);
    
    for i:= 1 to 10 do
        for j := 1 to 10 do
            writeln(b[i][j]);    
end.
