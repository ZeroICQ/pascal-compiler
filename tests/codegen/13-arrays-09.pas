var a,b : array[1..10] of array[1..10] of char;
    i,j:integer;
begin
    for i := 1 to 10 do
        for j := 1 to 10 do
            if ((j+i) mod 3) = 0 then
                a[i,j] := 'z'
            else if ((j+i) mod 3) = 1 then
                 a[i,j] := 'f'
            else
                a[i,j] := 'a';


    for i := 1 to 10 do 
    begin
        for j := 1 to 10 do
            write(a[i,j], ' ');
        writeln;
    end;
    writeln;    
            
    for i := 1 to 10 do 
    begin
        for j := 1 to 10 do
            write(b[i,j], ' ');
        writeln;
    end;
    writeln;    
    
    b := a;
    
    for i := 1 to 10 do 
    begin
        for j := 1 to 10 do
            write(b[i,j], ' ');
        writeln;
    end;
            
            
end.
