var a,b : array[1..10] of array[1..10] of char;
    i,j:integer;
begin
    for i := 1 to 10 do
        for j := 1 to 10 do
            if (j+i div 2) <> 0 then
                a[i,j] := 'z'
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
    
    b[4] := a[4];
    b[1] := a[1];
    
    for i := 1 to 10 do 
    begin
        for j := 1 to 10 do
            write(b[i,j], ' ');
        writeln;
    end;
            
            
end.
