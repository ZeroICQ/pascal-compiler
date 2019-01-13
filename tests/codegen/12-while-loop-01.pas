var
    i, k: integer;
begin
    while i <> 100 do begin
        i += 1;
        k += 3;
    end;
    writeln(k);


    i := 0;
    k := 0;
    while i <> 1000 do begin
        i += 1;
        k += 3;
        if i = 100 then
            break;
    end;
    writeln(k);


    i := 0;
    k := 0;
    while i <> 200 do begin
        i += 1;
        if i mod 2 <> 0 then
            continue;
        k += 3;
        while 1 = 1 do
            break;
    end;
    writeln(k);


    while 0 = 1 do
        writeln(0);
end.