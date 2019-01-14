var a,b : array[2..20] of array[12..90] of integer;
    i,j:integer;
begin
    for i := 2 to 20 do
        for j := 12 to 90 do
            a[i,j] := j*i;
            
            
    for i := 2 to 20 do
        b[i] := a[i];
    
    for i := 2 to 20 do
            for j := 12 to 90 do
                writeln(a[i,j], ' ', b[i,j]);
    

end.
