type 
    student = record
        age: Integer;
        lastGrade : char;
        salary: Double;
        History: array[1..3] of array[1..4] of char;
        grades: array[1..3] of array[1..4] of double;
    end;

var 
    studentA,studentB: student;
    i,j: integer;
begin
    studentA.age := 200;
    studentA.lastGrade := 'f';
    studentA.salary := -229.2;
    
    for i := 1 to 3 do
        for j := 1 to 4 do
            studentA.history[i,j] := 'n';
            
            
    for i := 1 to 3 do
        for j := 1 to 4 do
            studentA.grades[i,j] := i*j;
            
            
    writeln(studentA.age);
    writeln(studentA.lastGrade);
    writeln(studentA.salary);
    
    for i := 1 to 3 do
        for j := 1 to 4 do
            write(studentA.history[i,j], ' ');
            
            
    for i := 1 to 3 do
        for j := 1 to 4 do
            write(studentA.grades[i,j], ' ');
            
    writeln;        
    
    writeln(studentB.age);
    writeln(studentb.lastGrade);
    writeln(studentb.salary);
    
    for i := 1 to 3 do
        for j := 1 to 4 do
            write(studentb.history[i,j], ' ');
            
            
    for i := 1 to 3 do
        for j := 1 to 4 do
            write(studentb.grades[i,j], ' ');
            
    studentb := studenta;
    
    writeln(studentA.age);
    writeln(studentA.lastGrade);
    writeln(studentA.salary);
    
    for i := 1 to 3 do
        for j := 1 to 4 do
            write(studentA.history[i,j], ' ');
            
            
    for i := 1 to 3 do
        for j := 1 to 4 do
            write(studentA.grades[i,j], ' ');
            
    writeln;        
    
    writeln(studentB.age);
    writeln(studentb.lastGrade);
    writeln(studentb.salary);
    
    for i := 1 to 3 do
        for j := 1 to 4 do
            write(studentb.history[i,j], ' ');
            
            
    for i := 1 to 3 do
        for j := 1 to 4 do
            write(studentb.grades[i,j], ' ');
            
    writeln;
    
end.