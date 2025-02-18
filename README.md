# Shader Finder & Replacer Tool

**Shader Finder & Replacer Tool** is a Unity Editor extension designed to streamline the process of finding and updating shaders across your project. Whether you're migrating from URP to a different rendering pipeline, or simply ensuring consistency across your materials, this tool helps you quickly locate materials by shader and batch replace them with a new shader when necessary.

## Key Features

- **Find Materials by Shader:**  
  Search your entire project for materials using the specified shader. Options are available to include or filter out embedded materials.  
  _Originally implemented by **Jake Carter**._

- **Replace Shaders:**  
  Easily replace the shader in all relevant materials with a new shader in a single operation, with Undo support for safe modifications.

- **User-Friendly Editor Interface:**  
  Clean, tab-based UI separates the "Find Materials" and "Replace Shaders" functions and features custom styling for an optimized workflow.

- **Advanced Pagination:**  
  Navigate large sets of materials with a single horizontal scrollable pagination bar complete with previous and next buttons.

- **Material Previews & Selection:**  
  View previews of your materials directly within the tool and quickly select them for further inspection.

## Getting Started

1. **Installation:**  
   Place the `ShaderTool.cs` file into an `Editor` folder within your Unity project's `Assets` directory.

2. **Access the Tool:**  
   In the Unity Editor, navigate to **Tools > Search Material By Shader** to open the tool.

3. **Usage:**  
   - Use the **Find Materials** tab to search for materials using the desired shader.
   - Switch to the **Replace Shaders** tab when you need to update shaders in your materials.
   - Navigate through results using the horizontal scrollable pagination view and the previous/next buttons.

## Credits

- **[Jake Carter](https://jcfolio.weebly.com/):**
  The initial implementation of the "Find Materials" functionality was created by Jake Carter and is credited accordingly.

- **Muhammad Humza Butt:**  
  Extended and enhanced the original tool with additional features like shader replacement, improved pagination, and an updated user interface.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for more information.

---

Feel free to contribute, report issues, or suggest new features!
