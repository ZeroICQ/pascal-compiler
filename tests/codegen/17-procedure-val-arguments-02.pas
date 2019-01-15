type
    arr = array[1..10] of array[1..10] of char;

var i, k: integer;

procedure foo(c:char; g: integer; m:arr);
begin
    writeln(c, g);
    for i := 1 to 10 do
        for k := 1 to 10 do begin
            writeln(m[i][k]);
            m[i][k] := 'a'; 
            end;
end;

var kk:arr;
begin
    for i := 1 to 10 do
        for k := 1 to 10 do
             kk[i][k] := 'z';
                
    foo('a', 12, kk);
    
    for i := 1 to 10 do
        for k := 1 to 10 do begin
            writeln(kk[i][k]);
        end;
end.