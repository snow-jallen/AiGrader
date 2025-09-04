# AI Grader - Canvas Assignment Analyzer

A Blazor Server application that connects to Canvas LMS to download assignment submissions and analyze them using AI.

## Project Status: In Progress

### What's Been Completed:

1. **✅ Project Structure Created**
   - Blazor Server project with proper folder structure
   - Basic csproj file with required dependencies (Microsoft.Extensions.Http, System.Text.Json)
   - Program.cs configured with services registration

2. **✅ Canvas API Integration**
   - Models created (`CanvasModels.cs`): Assignment, Submission, Attachment, User
   - Canvas API service interface and implementation (`ICanvasApiService`, `CanvasApiService`)
   - Supports parsing Canvas URLs, fetching assignments, submissions, users, and downloading attachments
   - Handles pagination for large submission sets

3. **✅ AI Analysis System**
   - Analysis models (`AnalysisModels.cs`): SubmissionAnalysis, OverallAnalysis, SimilarityMatch, etc.
   - AI analysis service (`IAiAnalysisService`, `AiAnalysisService`)
   - Features:
     - Individual submission analysis
     - Standout detection (extremely short/long submissions)
     - Similarity detection between submissions
     - Overall summary generation
     - Statistics calculation

4. **✅ Blazor UI**
   - Main layout and routing configured
   - Index page with full workflow UI
   - Features:
     - Assignment URL input
     - Progress indicators during processing
     - Results display with statistics, summaries, standouts, and similarities
     - Error handling and reset functionality

5. **✅ Configuration**
   - `appsettings.json` configured with AI model settings (URL and name)
   - User secrets ID added to project file for Canvas API token

### What Still Needs to Be Done:

6. **🔄 User Secrets Setup** (Currently in progress)
   - Initialize user secrets
   - Set Canvas API token: `dotnet user-secrets set "CanvasApiToken" "YOUR_TOKEN_HERE"`
   - Set OpenAI API key: `dotnet user-secrets set "OpenAiApiKey" "YOUR_OPENAI_KEY_HERE"`

7. **⏳ Testing and Validation**
   - Test the complete workflow with a real Canvas assignment
   - Verify Canvas API integration works
   - Test AI analysis functionality
   - Handle edge cases and error scenarios

## How to Continue:

1. **Set up user secrets:**
   ```bash
   cd d:\AiGrader
   dotnet user-secrets init
   dotnet user-secrets set "CanvasApiToken" "your_canvas_api_token_here"
   dotnet user-secrets set "OpenAiApiKey" "your_openai_api_key_here"
   ```

2. **Run the application:**
   ```bash
   dotnet run
   ```

3. **Test with a Canvas assignment URL like:**
   `https://snow.instructure.com/courses/1155292/assignments/16826744`

## Key Files Created:
- `AiGrader.csproj` - Project file with dependencies
- `Program.cs` - Application startup and service configuration
- `appsettings.json` - Configuration with AI model settings
- `Models/CanvasModels.cs` - Canvas API data models
- `Models/AnalysisModels.cs` - AI analysis data models
- `Services/ICanvasApiService.cs` & `CanvasApiService.cs` - Canvas API integration
- `Services/IAiAnalysisService.cs` & `AiAnalysisService.cs` - AI analysis logic
- `Pages/Index.razor` - Main UI page
- `App.razor`, `_Host.cshtml`, `Shared/MainLayout.razor` - Blazor infrastructure
- `wwwroot/css/site.css` & `bootstrap/bootstrap.min.css` - Styling

## Configuration Notes:
- Canvas API token stored in user secrets as "CanvasApiToken"
- OpenAI API key stored in user secrets as "OpenAiApiKey"
- AI model configured in appsettings.json (currently set to OpenAI GPT-4o-mini)
- Supports Canvas instance at snow.instructure.com (configurable in service)