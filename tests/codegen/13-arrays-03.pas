var
   a: array [-100..300] of array[10..10] of array[10..13] of char;
   i,j,k,l : integer;

begin
   for i :=-100 to 300 do
      for j := 10 to 10 do
         for k := 10 to 13 do
            if i >=100 then
               a[i,j,k] := 'a'
            else 
               a[i,j,k] := 'z';

   for i :=-100 to 300 do
      for j := 10 to 10 do
         for k := 10 to 13 do
            write(a[i,j,k])

end.