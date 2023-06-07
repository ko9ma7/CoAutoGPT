# Collaborative AutoGPT (CoAutoGPT) Agent

The Collaborative AutoGPT (CoAutoGPT) is an exploratory project that introduces a novel ChatGPT-based reflective agent, aimed at addressing and resolving issues of high complexity. This project is designed to enhance human-computer collaboration by augmenting human inputs with the robust capabilities of autoGPT. The underlying objective is to streamline the efficiency of collaboration, while also equipping the system to address large-scale multi-task problems.

In this setup, the human collaborator assumes a role analogous to that of a project manager or business analyst. They provide a broad task description and guide the agent through the process, fine-tuning the scope of activities performed by the agent. This synergistic workflow minimizes human input required while simultaneously enabling them to guide the process effectively.

The CoAutoGPT employs a biologically-inspired dual system approach. For less complex tasks, the agent leverages zero-shot reasoning. For more intricate subtasks, it applies an elaborative reasoning system. The agent uniquely uses a complexity-based approach, allowing it to determine the most suitable reasoning method for a given subtask.

For handling complex problems, the CoAutoGPT implements a graph-based memory system. This system decomposes a problem into easier-to-solve subtasks, the solutions to which are then synthesized to arrive at the final product. The agent's state can be preserved, granting the user a significant degree of flexibility when working on extensive problems that may require revisiting and refining initial concepts as the agent progresses.

At its current iteration, the CoAutoGPT does not support the utilization of external tools or web resources. However, future releases are intended to incorporate these capabilities. An exemplary task that the system can effectively address is large book and paper creation. As we look forward, the addition of external tools and updates will enable the CoAutoGPT to be utilized in other areas such as software application development.

## Table of Contents
1. [Installation](#installation)
2. [Usage](#usage)
3. [Contributing](#contributing)
4. [License](#license)
5. [Contact](#contact)

## Installation

To run this experiment you will need to set your OpenAI API key in your local secrets store with the following steps:

1. Open command line terminal in the root directory of the project
2. Execute the following command: `dotnet user-secrets init`
3. Execute the following command: `dotnet user-secrets set "OPENAPIKEY" "YOUR_API_KEY"`

## Usage Guidelines

The CoAutoGPT experimental project boasts a user-friendly command line interface, enabling users to conveniently edit or delete subtasks proposed by the agent. Additionally, users can provide feedback on the outcomes of subtasks, signaling that a task is complete, prompting the agent to expand, or directing the agent to expand based on user feedback.

The central entry point of the application, `Program.cs`, contains a field called `taskDescription`. This field should be updated to reflect the description of the problem you wish to address. Instructions for usage and potential interactions with the system are as follows:

1. **Edit Subtasks:** To edit a proposed subtask, utilize the `edit` command followed by the identifier of the subtask.
2. **Delete Subtasks:** The `delete` command followed by the subtask identifier will remove the subtask.
3. **Provide Feedback:** Users can provide feedback on the results of subtasks using the `feedback` command. This command can be followed by `done` to indicate task completion, `expand` to request the agent to develop the subtask further, or `expand with feedback` to direct the agent

 to expand based on specific user feedback.

## Contributing

If you are interested in contributing, please drop us an email at [admin@substrate.ai](mailto:admin@substrate.ai).

## License

MIT License

## Contact

Bren Worth: [bworth@substrate.ai](mailto:bworth@substrate.ai)
