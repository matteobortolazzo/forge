using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Claude.CodeSdk.Mock;

/// <summary>
/// Provides scenario management for mock Claude agent clients.
/// Singleton that holds scenarios and matches prompts to scenarios.
/// </summary>
public sealed class MockScenarioProvider
{
    private readonly ConcurrentDictionary<string, MockScenario> _scenarios = new();
    private readonly ConcurrentDictionary<string, string> _patternMappings = new();
    private string _defaultScenarioId = "default";

    /// <summary>
    /// Initializes a new instance of the <see cref="MockScenarioProvider"/> class
    /// with pre-built scenarios registered.
    /// </summary>
    public MockScenarioProvider()
    {
        // Register all pre-built scenarios
        foreach (var scenario in MockScenario.AllScenarios)
        {
            RegisterScenario(scenario);
        }
    }

    /// <summary>
    /// Registers a scenario for use.
    /// </summary>
    /// <param name="scenario">The scenario to register.</param>
    public void RegisterScenario(MockScenario scenario)
    {
        _scenarios[scenario.Id] = scenario;
    }

    /// <summary>
    /// Sets the default scenario used when no pattern matches.
    /// </summary>
    /// <param name="scenarioId">The scenario ID to use as default.</param>
    /// <exception cref="ArgumentException">Thrown when scenario ID is not found.</exception>
    public void SetDefaultScenario(string scenarioId)
    {
        if (!_scenarios.ContainsKey(scenarioId))
        {
            throw new ArgumentException($"Scenario '{scenarioId}' not found", nameof(scenarioId));
        }

        _defaultScenarioId = scenarioId;
    }

    /// <summary>
    /// Maps a regex pattern to a scenario ID.
    /// When a prompt matches the pattern, the corresponding scenario is used.
    /// </summary>
    /// <param name="pattern">The regex pattern to match against prompts.</param>
    /// <param name="scenarioId">The scenario ID to use when pattern matches.</param>
    /// <exception cref="ArgumentException">Thrown when scenario ID is not found.</exception>
    public void MapPatternToScenario(string pattern, string scenarioId)
    {
        if (!_scenarios.ContainsKey(scenarioId))
        {
            throw new ArgumentException($"Scenario '{scenarioId}' not found", nameof(scenarioId));
        }

        _patternMappings[pattern] = scenarioId;
    }

    /// <summary>
    /// Removes a pattern mapping.
    /// </summary>
    /// <param name="pattern">The pattern to remove.</param>
    public void RemovePatternMapping(string pattern)
    {
        _patternMappings.TryRemove(pattern, out _);
    }

    /// <summary>
    /// Gets the scenario that should be used for the given prompt.
    /// </summary>
    /// <param name="prompt">The prompt to match.</param>
    /// <returns>The matched scenario.</returns>
    public MockScenario GetScenarioForPrompt(string prompt)
    {
        // Check pattern mappings first
        foreach (var (pattern, scenarioId) in _patternMappings)
        {
            try
            {
                if (Regex.IsMatch(prompt, pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)))
                {
                    if (_scenarios.TryGetValue(scenarioId, out var matchedScenario))
                    {
                        return matchedScenario;
                    }
                }
            }
            catch (RegexMatchTimeoutException)
            {
                // Skip invalid patterns
            }
        }

        // Fall back to default scenario
        return _scenarios.TryGetValue(_defaultScenarioId, out var defaultScenario)
            ? defaultScenario
            : MockScenario.Default;
    }

    /// <summary>
    /// Gets a scenario by its ID.
    /// </summary>
    /// <param name="scenarioId">The scenario ID.</param>
    /// <returns>The scenario if found; otherwise, null.</returns>
    public MockScenario? GetScenario(string scenarioId)
    {
        return _scenarios.TryGetValue(scenarioId, out var scenario) ? scenario : null;
    }

    /// <summary>
    /// Gets all registered scenarios.
    /// </summary>
    public IReadOnlyCollection<MockScenario> GetAllScenarios()
    {
        return _scenarios.Values.ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets the current default scenario ID.
    /// </summary>
    public string DefaultScenarioId => _defaultScenarioId;

    /// <summary>
    /// Resets the provider to default state.
    /// </summary>
    public void Reset()
    {
        _patternMappings.Clear();
        _defaultScenarioId = "default";
    }
}
