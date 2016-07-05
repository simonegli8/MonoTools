using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CSharp;
using System.Dynamic;

namespace MonoTools.Server.Setup {

	public class Window: DynamicObject, IEnumerable<Window.Input> {

		public ConsoleColor Color = ConsoleColor.Black;
		public ConsoleColor BackColor = ConsoleColor.Gray;
		public ConsoleColor InputColor = ConsoleColor.White;
		public ConsoleColor InputBackColor = ConsoleColor.DarkGray;
		public ConsoleColor FocusColor = ConsoleColor.White;
		public ConsoleColor FocusBackColor = ConsoleColor.Black;
		public ConsoleColor FocusButtonColor = ConsoleColor.White;
		public ConsoleColor FocusButtonBackColor = ConsoleColor.DarkGreen;
		public ConsoleColor ProgressColor = ConsoleColor.DarkGreen;
		public ConsoleColor OldColor = System.Console.ForegroundColor;
		public ConsoleColor OldBackColor = System.Console.BackgroundColor;
		public const bool ASCII = true;
		public const bool Border = false;

		InputsList Inputs = new InputsList();

		public override bool TryGetMember(GetMemberBinder binder, out object result) {
			var res = Inputs.Contains(binder.Name);
			if (res) result = Inputs[binder.Name];
			else result = null;
			return res;
		}
		public string Button;
		public Input First, Last;
		public Input Focus = null;
		public Input Default => Inputs.FirstOrDefault(p => p.Default);
		public Input Cancel => Inputs.FirstOrDefault(p => p.Cancel);
		public void Add(Input input) {
			Inputs.Add(input);
			if (First == null) {
				First = input;
				Last = input;
				Focus = First;
			}
			input.Next = First;
			input.Prev = Last;
			Last.Next = input;
			First.Prev = input;
			Last = input;
		}

		IEnumerator<Window.Input> IEnumerable<Window.Input>.GetEnumerator() {
			return Inputs.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator() {
			return ((IEnumerable)Inputs).GetEnumerator();
		}

		// Input elements
		public class InputsList : KeyedCollection<string, Input> {
			protected override string GetKeyForItem(Input item) => item.Name;
		}

		public class Input {
			public Window Window;
			public int Top, Left;
			public string Id;
			public string Name;
			public int Width => DefaulText.Length;
			public bool Button;
			public string DefaulText;
			public int CursorPos;
			public bool Default;
			public bool Cancel;
			public bool Selected;
			public int CursorPosition;
			double progress = -1;
			public double Progress {
				get { return progress; }
				set {
					if (value == double.MinValue) value = -1;
					else if (value < 0) value = 1/(value*4-1)+1;
					else if (value > 1) value = 1;
					if (progress != value) {
						progress = value;
						Show();
					}
				}
			}
			public bool Password;
			public string Value = "";
			public bool CanFocus = true;
			public Input Next, Prev;
			public bool IsFocus => Window.Focus == this;
			public Input Focus {
				get { return Window.Focus; }
				set {
					if (value.CanFocus) {
						Window.Focus = value; Show();
					} else if (value == Window.Focus.Prev) {
						Window.Focus = value;
						Focus = value.Prev;
					} else {
						Window.Focus = value;
						Focus = value.Next;
					}
				}
			}

			public void Show() {
				System.Console.CursorVisible = false;
				System.Console.CursorTop = Top; System.Console.CursorLeft = Left;
				if (Progress >= 0) { // progress bar
					System.Console.ForegroundColor = IsFocus ? Window.FocusColor : Window.InputColor;
					var n = (int)(Width*Progress+0.5);
					var p = ((int)(Progress*100+0.5)).ToString()+"%";
					var t = (Width - p.Length)/2;
					for (int i = 0; i < Width; i++) {
						System.Console.BackgroundColor = i < n ? Window.ProgressColor : Window.InputBackColor;
						System.Console.Write((i < t || i >= t + p.Length) ? ' ' : p[i-t]);
					}
				} else if (!Button) {
					System.Console.ForegroundColor = IsFocus ? Window.FocusColor : Window.InputColor;
					System.Console.BackgroundColor = IsFocus ? Window.FocusBackColor : Window.InputBackColor;
					int n = Value.Length;
					if (!Password) System.Console.Write(Value);
					else {
						while (n-- > 0) System.Console.Write("*");
					}
					n = Width - Value.Length;
					while (n-- > 0) System.Console.Write(" ");
					System.Console.CursorLeft = Left + CursorPosition;
					System.Console.CursorVisible = true;
				} else {
					System.Console.ForegroundColor = IsFocus ? Window.FocusButtonColor : Window.InputColor;
					System.Console.BackgroundColor = IsFocus ? Window.FocusButtonBackColor : Window.InputBackColor;
					System.Console.Write(DefaulText);
					System.Console.CursorLeft = Left;
				}
				System.Console.BackgroundColor = Window.OldBackColor;
				System.Console.ForegroundColor = Window.OldColor;
			}

			public void Type(ConsoleKeyInfo key) {
				var pos = CursorPosition;
				if (key.Key == ConsoleKey.Backspace) {
					if (Value.Length > 0 && pos > 0) Value = Value.Substring(0, pos-1) + Value.Substring(pos--);
				} else if (key.Key == ConsoleKey.Delete) {
					if (Value.Length > 0 && pos < Value.Length) Value = Value.Substring(0, pos) + Value.Substring(pos+1);
				} else if (key.Key == ConsoleKey.LeftArrow) {
					if (pos > 0) pos--;
				} else if (key.Key == ConsoleKey.RightArrow) {
					if (pos < Value.Length) pos++;
				} else Value = Value.Substring(0, pos) + key.KeyChar + Value.Substring(pos++);
				CursorPosition = pos;
				Show();
			}
			public bool Submit() {
				if (Button) {
					Window.Button = Name;
					Selected = true;
				}
				return !Button;
			}

			public bool Edit() {
				Focus = this;
				Show();
				var key = System.Console.ReadKey();
				switch (key.Key) {
				case ConsoleKey.Enter:
					if (Button) return Submit();
					else if (Window.Default != null) return Window.Default.Submit();
					else Focus = Focus.Next;
					break;
				case ConsoleKey.Tab:
					if ((key.Modifiers & ConsoleModifiers.Shift) != 0) Focus = Focus.Prev;
					else Focus = Focus.Next;
					break;
				case ConsoleKey.UpArrow:
					Focus = Focus.Prev;
					break;
				case ConsoleKey.DownArrow:
					Focus = Focus.Next;
					break;
				case ConsoleKey.LeftArrow:
					if (Focus.Button) Focus = Focus.Prev;
					else Type(key);
					break;
				case ConsoleKey.RightArrow:
					if (Focus.Button) Focus = Focus.Next;
					else Type(key);
					break;
				case ConsoleKey.Escape:
					if (Window.Cancel != null) return Window.Cancel.Submit();
					break;

				default:
					Type(key);
					break;
				}
				return true;
			}
		}

		public dynamic Clear() {
			System.Console.CursorVisible = false;
			System.Console.ForegroundColor = Color;
			System.Console.BackgroundColor = BackColor;
			System.Console.CursorTop = 0;
			System.Console.CursorLeft = 0;
			int n; 
			if (Border) {
				n = System.Console.WindowWidth;
				System.Console.Write(ASCII ? "#" : "╔");
				while (n-- > 2) System.Console.Write(ASCII ? "=" : "═");
				System.Console.Write(ASCII ? "#" : "╗");
				System.Console.CursorTop = 1;
				n = System.Console.WindowHeight-2;
				while (n-- > 0) {
					System.Console.CursorLeft = 0;
					System.Console.Write(ASCII ? "|" : "║");
					var w = System.Console.WindowWidth-2;
					while (w-- > 0) System.Console.Write(' ');
					System.Console.Write(ASCII ? "|" : "║");
				}
				System.Console.CursorLeft = 0;
				n = System.Console.WindowWidth;
				System.Console.Write(ASCII ? "#" : "╚");
				while (n-- > 2) System.Console.Write(ASCII ? "=" : "═");
				System.Console.Write(ASCII ? "#" : "╝");
				System.Console.CursorTop = 0;
				System.Console.CursorLeft = 0;
				System.Console.Write(ASCII ? "#" : "╔");
				System.Console.CursorLeft = 1;
				System.Console.CursorTop = 2;
				System.Console.CursorSize = 10;
			} else {
				n = System.Console.WindowHeight;
				while (n-- > 0) {
					var m = System.Console.WindowWidth;
					while (m-- > 0) System.Console.Write(' ');
				}
			}
			return this;
		}

		public dynamic Close() { System.Console.Clear(); return this; }

		public dynamic Edit() {
			if (Focus != null) {
				while (Focus.Edit()) ;
			}
			return this;
		}


		/// <summary>
		/// Shows a console dialog. 
		/// </summary>
		/// <param name="text">The text conatins a dialog template. Usual text is displayed as in text, with the follwing options:
		/// - Input fields can be expressed as xml elements like this: &lt;ElementName&gt;Default text goes here...&lt;/ElementName&gt;
		/// - The input elements name contains special characters, to distinguish its type. There are following types defined:
		/// - If the element name contains a &amp;, then the input field is a regular button.
		/// - If the element name contains a !, then the input field is a default button.
		/// - If the element name contains a ~, then the input field is a cancel button.
		/// - If the element name contains a *, then the input field is a password field.
		/// - If the element name contains a %, then the field is a progress bar.
		/// - If the element name contains none of these, then it's an ordinary text input field.
		/// </param>
		/// <param name="defaults">An object (usualy an anonymous type), with properties or fiels corresponding to the inputs in the template, containing default values as strings.</param>
		/// <returns>Returns a dynamic object with fields named after the inputs in the template and of type Window.Input, so you can read out entered values, or set a progress bar's progess value.</returns>
		public dynamic Show(string text, object defaults = null) {
			Clear();
			var lines = text.Split('\n').Select(line => line.Trim('\r')).ToArray();
			int top;
			System.Console.CursorVisible = false;
			System.Console.CursorTop = top = (System.Console.WindowHeight - lines.Length) / 2;
			foreach (var line in lines) {
				bool leftJustify, rightJustify;
				leftJustify = line.StartsWith("|");
				rightJustify = line.EndsWith("|");
				var matches = Regex.Matches(line.Trim('|'), @"(?<input><(?<name>[!*&~%]*\w+)>(?<field>.*)</\k<name>>)|(?<text>.*?(?=(<(?<next>[!*&~%]*\w+)>(.*)</\k<next>>)|$))", RegexOptions.Multiline);
				var width = matches.OfType<Match>().Select(m => m.Groups["text"].Success ? m.Groups["text"].Value.Length : m.Groups["field"].Value.Length).Sum();
				int left;
				if (leftJustify) left = 0;
				else if (rightJustify) left = System.Console.WindowWidth - width;
				else left = (System.Console.WindowWidth - width) / 2;
				if (left < (Border ? 1 : 0)) left = Border ? 1 : 0;
				System.Console.CursorLeft = left;

				int offset = 0;
				foreach (Match m in matches) {
					if (m.Groups["name"].Success) {
						var id = m.Groups["name"].Value;
						var field = m.Groups["field"].Value;
						var name = id.Replace("*", "").Replace("!", "").Replace("&", "").Replace("~", "").Replace("%", "");
						var input = new Input() {
							Window = this,
							Top = top,
							Id =id,
							Left = left + m.Index - offset,
							Name = name,
							DefaulText = field,
							Default = id.Contains("!"),
							Button = id.Contains("&") || id.Contains("!") || id.Contains("~"),
							Cancel = id.Contains("~"),
							Password = id.Contains("*"),
							Progress = id.Contains("%") ? 0 : double.MinValue, 
							Value = defaults?.GetType().GetProperty(name)?.GetValue(defaults, null) as string ??
								defaults?.GetType().GetField(name)?.GetValue(defaults) as string ??
								field.Trim(),
							CursorPosition = field.Trim().Length,
							Selected = false
						};
						Add(input);
						//input.Show();
						offset += m.Groups["input"].Value.Length - input.Width;
					} else {
						System.Console.BackgroundColor = BackColor;
						System.Console.ForegroundColor = Color;
						System.Console.Write(m.Groups["text"].Value);
					}
				}
				System.Console.WriteLine();
				top++;
			}
			System.Console.CursorTop = Border ? 1 : 0;
			foreach (var input in Inputs) input.Show();
			Focus = Inputs.FirstOrDefault();
			return this;
		}

		public dynamic Dialog(string text, object defaults = null) {
			return Show(text, defaults).Edit().Close();
		}

		public static dynamic Open(string text, object defaults = null) {
			return new Window().Show(text, defaults);
		}

		public static dynamic OpenDialog(string text, object defaults = null) {
			return Open(text, defaults).Edit().Close();
		}
	}
}
