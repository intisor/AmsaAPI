app.UseStaticFiles();
app.UseRouting();

// Map Razor Pages
app.MapRazorPages();

app.UseFastEndpoints();

// Welcome message
app.MapGet("/", () => "Welcome to the AMSA Nigeria API! " +
    "FastEndpoints: /api/* | Minimal API: /api/minimal/* | Test: /test.html");
        <h3>?? Performance Analysis Ready</h3>
        <p>I've created a comprehensive benchmark that compares:</p>
        <ul>
            <li><strong>Data Transformation:</strong> Entity ? DTO mapping performance</li>
            <li><strong>Search Algorithms:</strong> Contains vs IndexOf vs SQL Like</li>
            <li><strong>Role Aggregation:</strong> LINQ vs GroupBy strategies</li>
            <li><strong>Memory Usage:</strong> Allocation patterns</li>
        </ul>
        
        <h3>?? To Run the Benchmark:</h3>
        <ol>
            <li>Stop your web application</li>
            <li>Open a terminal in your project directory</li>
            <li>Run: <code style='background: rgba(0,0,0,0.3); padding: 5px;'>dotnet run --configuration Release -- benchmark</code></li>
        </ol>
        
        <h3>?? What You'll Discover:</h3>
        <ul>
            <li>Performance differences between your two API implementations</li>
            <li>Memory allocation patterns in your data transformations</li>
            <li>Optimization opportunities for high-volume scenarios</li>
            <li>Best practices for your specific use cases</li>
        </ul>
        
        <p><em>This benchmark uses BenchmarkDotNet - the gold standard for .NET performance measurement!</em></p>
    </div>
    
    <div style='background: rgba(255,255,255,0.1); padding: 15px; border-radius: 10px; margin-top: 20px;'>
        <h3>?? Quick Preview</h3>
        <p>Your APIs handle complex hierarchical data (National ? State ? Unit ? Member) with role-based relationships. The benchmark will reveal which approaches work best for your data patterns!</p>
    </div>
</body>
</html>", "text/html");
}).WithTags("Benchmark");
=======
    "FastEndpoints: /api/* | Minimal API: /api/minimal/* | Test: /test.html");
>>>>>>> 091dd54 (feat: Remove BenchmarkDotNet references and update import response message handling)

// Map all minimal API endpoints
app.MapMemberEndpoints();
app.MapOrganizationEndpoints();
app.MapDepartmentEndpoints();
app.MapStatisticsEndpoints();
app.MapImportEndpoints();

// Note: benchmark runner removed

app.Run();
