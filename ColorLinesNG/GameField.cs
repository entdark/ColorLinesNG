using System;
using System.Collections.Generic;
using System.IO;

using Android.Graphics;
using Android.Content;
using Android.Views;
using Android.Content.Res;

using CLRenderer;

using SimpleAStarExample;

namespace ColorLinesNG {
	public enum CLColour {
		CLNone,
		CLRed,
		CLYellow,
		CLGreen,
		CLCyan,
		CLBlue,
		CLPink,
		CLBrown,
		CLMax,
	}
	public enum CLLabelSize {
		CLSmall,
		CLMiddle,
		CLLarge,
	}
	public class CLLabel {
		public Action Action, ExtraDraw, OutAction;
		private const float xLabelOffset = 0.12f;
		public string text;
		private float x, y, width, height, textSize, textInterval;
		private Paint.Align align;
		private Color textColour;
		public CLLabel(string text, float textSize, float x, float y, float width, float height, Paint.Align align, Color textColour, float textInterval = 1.337f) {
			this.text = text;
			this.x = x;
			this.y = y;
			this.width = width;
			this.height = height;
			this.textSize = textSize;
			this.align = align;
			this.textColour = textColour;
			this.textInterval = textInterval;
		}
		public void Draw(int textureId, float xOffset, float yOffset) {
			CLReDraw.Rect(this.x, this.y, this.width, this.height, textureId);
			DrawText(xOffset, yOffset);
		}
		public void DrawText(float xOffset, float yOffset) {
			if (this.align == Paint.Align.Right)
				CLReDraw.Text(this.text, this.x+this.width*(1.0f-xLabelOffset)+xOffset, this.y-this.height*2/3.2f-yOffset, this.textSize, this.textColour, this.align, this.textInterval);
			else if (this.align == Paint.Align.Center)
				CLReDraw.Text(this.text, this.x+this.width*0.5f+xOffset, this.y-this.height*2/3.2f-yOffset, this.textSize, this.textColour, this.align, this.textInterval);
			else
				CLReDraw.Text(this.text, this.x+this.width*xLabelOffset+xOffset, this.y-this.height*2/3.2f-yOffset, this.textSize, this.textColour, this.align, this.textInterval);
		}
		public bool HitLabel(float x, float y) {
			if (x >= this.x && x <= this.x+this.width
				&& y <= this.y && y >= this.y-this.height)
				return true;
			return false;
		}
	}
	public class CLCell {
		public CLCell top, bottom, left, right;
		public CLColour colour;
		public int row, column;
		public bool selected;
		//cell is a four linked list
		public CLCell(int row, int column, CLCell top = null, CLCell bottom = null, CLCell left = null, CLCell right = null) {
			this.colour = CLColour.CLNone;
			this.row = row;
			this.column = column;
			this.selected = false;
			this.top = top;
			this.bottom = bottom;
			this.left = left;
			this.right = right;
			if (top != null) top.bottom = this;
			if (bottom != null) bottom.top = this;
			if (left != null) left.right = this;
			if (right != null) right.left = this;
		}

		private Color CLColourToColor(CLColour colour) {
			if (colour == CLColour.CLRed)
				return Color.Argb(255, 255, 0, 0);
			else if (colour == CLColour.CLYellow)
				return Color.Argb(255, 255, 255, 0);
			else if (colour == CLColour.CLGreen)
				return Color.Argb(255, 0, 255, 0);
			else if (colour == CLColour.CLCyan)
				return Color.Argb(255, 0, 255, 255);
			else if (colour == CLColour.CLBlue)
				return Color.Argb(255, 0, 0, 255);
			else if (colour == CLColour.CLPink)
				return Color.Argb(255, 255, 0, 255);
			else if (colour == CLColour.CLBrown)
				return Color.Argb(255, 63, 15, 0);
			else
				return Color.Argb(255, 127, 127, 127);
		}
		public void Draw(float x, float y, float width, float height, int []fieldTextures) {
			int index = (int)this.colour;
			int textureId = fieldTextures[index];
			if (!this.selected)
//				CLReDraw.Rect(x, y, width, height, Color.Argb(255, 127, 127, 127));
				CLReDraw.Rect(x, y, width, height, fieldTextures[0]);
			else
				CLReDraw.Rect(x, y, width, height, fieldTextures[0], new float[] {
					1, 0,
					0, 0,
					1, 1,
					0, 1,
				});
			if (this.colour != CLColour.CLNone)
//				CLReDraw.Circle(x+width/2, y-height/2, (height > width) ? (width * 0.35f) : (height * 0.35f), CLColourToColor(this.colour));
				CLReDraw.Rect(x, y, width, height, textureId);
		}
		public void DrawSelected() {

		}
	}
	public class CLField {
		private const int bestScoresCount = 10;
		private CLCell cells, selected;
		private int rows, columns;
		private int cellsCount = 0;
		private CLColour []nextColours;
		private CLLabel bestScore, userScore, results, start, exit, popUp;
		private float left, right, bottom, top, textSize;
		public bool quit;
		private int ballsCount;
		private int score;
		private string [,]scoresTable;
//		private Dictionary<int, string> bestScores;
		public int []fieldTextures;
		public CLField(int []fieldTextures, Context context) : this(9, 9, fieldTextures, context) {
		}
		public CLField(int rows, int columns, int []fieldTextures, Context context) {
			CLCell c = this.cells = new CLCell(0, 0);
			this.cellsCount++;
			for (int i = 0; i < rows; i++) {
				for (int j = 0; j < columns; j++) {
					if (i == 0 && j == 0)
						continue;
					CLCell ocell = null;
					if (i > 0) {
						ocell = this.cells;
						for (int k = 1; k < i; k++)
							ocell = ocell.bottom;
						for (int l = 0; l < j; l++)
							ocell = ocell.right;
					}
					if (j == 0)
						c = null;
					CLCell cell = new CLCell(i, j, top: ocell, left: c);
					this.cellsCount++;
					c = cell;
				}
			}
			c = this.cells;
			this.selected = null;
			this.rows = rows;
			this.columns = columns;
			this.score = 0;
			this.scoresTable = LoadBestScores(context);
			this.fieldTextures = fieldTextures;
			this.nextColours = new CLColour[3];
			this.popUp = null;
			this.quit = false;
			Random r = new Random(DateTime.Now.Millisecond);
			for (uint i = 0; i < 3; i++) {
				this.nextColours[i] = (CLColour)r.Next((int)CLColour.CLRed, (int)CLColour.CLMax);
			}
			AddBalls(true);
		}
		private string [,]LoadBestScores(Context context) {
			string []bsS = new string[bestScoresCount*2] {
				"user", "0", "user", "0", "user", "0", "user", "0", "user", "0",
				"user", "0", "user", "0", "user", "0", "user", "0", "user", "0",
			};
			string [,]bsD = new string[bestScoresCount, 2];
/*			try { using (System.IO.StreamReader sr = new System.IO.StreamReader(context.Assets.Open(""))) {
				bsS = sr.BaseStream.ToString().Split('\n');
			}} catch {}*/
			string appFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			string scoresFile = System.IO.Path.Combine(appFolder, "scores");
			if (File.Exists(scoresFile)) {
				using (FileStream fs = new FileStream(scoresFile, FileMode.Open, FileAccess.Read)) {
					byte []buffer = new byte[300]; //actually 290, but let's alloc 300 to be sure
					fs.Read(buffer, 0, 300);
					string []bs = GetString(buffer).Split('\n');
					if (bs.Length >= bestScoresCount)
						bsS = bs;
				}
			}
			for (int i = 0; i < bestScoresCount; i++) {
				bsD[i, 0] = bsS[i*2];
				bsD[i, 1] = bsS[i*2+1];
			}
			return bsD;
		}
		
		public void Draw(bool landscape, float left, float right, float bottom, float top) {
			float step;
//			CLReDraw.Rect(-1.0f, 1.0f, 2.0f, 2.0f, Color.Argb(255, 127, 127, 127));
			if (this.rows > this.columns)
				step = 2.0f / this.rows;
			else
				step = 2.0f / this.columns;
			CLCell cell = this.cells;
			for (int i = 0; i < this.rows; i++, cell = cell.bottom) {
				for (int j = 0; j < this.columns; j++, cell = cell.right) {
					if (cell == null)
						break;
					cell.Draw(j*step-1.0f, 1.0f-i*step, step, step, this.fieldTextures);
				}
				cell = this.cells;
				for (int k = 0; k < i; k++, cell = cell.bottom);
				if (cell == null)
					break;
			}
			float radius = step / 5.0f;
			if (!landscape) {
				CLReDraw.Rect(left+step*3, top, step, step, this.fieldTextures[0]);
				if (this.nextColours[0] != CLColour.CLNone)
					CLReDraw.Rect(left+step*3.5f-radius, top-step*0.5f+radius, radius*2, radius*2, this.fieldTextures[(int)this.nextColours[0]]);
				CLReDraw.Rect(left+step*4, top, step, step, this.fieldTextures[0]);
				if (this.nextColours[1] != CLColour.CLNone)
					CLReDraw.Rect(left+step*4.5f-radius, top-step*0.5f+radius, radius*2, radius*2, this.fieldTextures[(int)this.nextColours[1]]);
				CLReDraw.Rect(left+step*5, top, step, step, this.fieldTextures[0]);
				if (this.nextColours[2] != CLColour.CLNone)
					CLReDraw.Rect(left+step*5.5f-radius, top-step*0.5f+radius, radius*2, radius*2, this.fieldTextures[(int)this.nextColours[2]]);
			} else {
				CLReDraw.Rect(left, top, step, step, this.fieldTextures[0]);
				if (this.nextColours[0] != CLColour.CLNone)
					CLReDraw.Rect(left+step*0.5f-radius, top-step*0.5f+radius, radius*2, radius*2, this.fieldTextures[(int)this.nextColours[0]]);
				CLReDraw.Rect(left+step, top, step, step, this.fieldTextures[0]);
				if (this.nextColours[1] != CLColour.CLNone)
					CLReDraw.Rect(left+step*1.5f-radius, top-step*0.5f+radius, radius*2, radius*2, this.fieldTextures[(int)this.nextColours[1]]);
				CLReDraw.Rect(left+step*2, top, step, step, this.fieldTextures[0]);
				if (this.nextColours[2] != CLColour.CLNone)
					CLReDraw.Rect(left+step*2.5f-radius, top-step*0.5f+radius, radius*2, radius*2, this.fieldTextures[(int)this.nextColours[2]]);
			}
			int textureId = (int)CLColour.CLMax+(int)CLLabelSize.CLSmall;
			this.bestScore.Draw(this.fieldTextures[textureId], 0.0f, 0.0f);
			this.userScore.Draw(this.fieldTextures[textureId], 0.0f, 0.0f);
			this.results.Draw(this.fieldTextures[textureId], 0.0f, 0.0f);
			this.start.Draw(this.fieldTextures[textureId], 0.0f, 0.0f);
			this.exit.Draw(this.fieldTextures[textureId], 0.0f, 0.0f);
			if (this.popUp != null)
				this.popUp.ExtraDraw();
		}
		public void DrawMove() {

		}
		
		private int AddScore(int ballsCount) {
			if (ballsCount >= 9)
				return 42;
			else if (ballsCount == 8)
				return 28;
			else if (ballsCount == 7)
				return 18;
			else if (ballsCount == 6)
				return 12;
			else if (ballsCount == 5)
				return 10;
			return 0;
		}

		private string GetString(int id) {
			return GLView.context.Resources.GetString(id);
		}
		public void MakeLabels(bool landscape, float left, float right, float bottom, float top, float scale) {
			this.left = left;
			this.right = right;
			this.bottom = bottom;
			this.top = top;
			float step;
			if (this.rows > this.columns)
				step = 2.0f / this.rows;
			else
				step = 2.0f / this.columns;
			float width = step*3, height = step;
			float hor = right - left,
				ver = top - bottom;
			if (ver > hor)
				this.textSize = height * 1.11125f * scale;
			else
				this.textSize = height * 1.11125f * scale;
			if (!landscape) {
				this.bestScore = new CLLabel(this.scoresTable[0,1], this.textSize, left, top, width, height, Paint.Align.Right, Color.Argb(255, 0, 170, 0));
				this.userScore = new CLLabel(this.score.ToString(), this.textSize, left+step*6, top, width, height, Paint.Align.Right, Color.Argb(255, 0, 170, 0));
				this.results = new CLLabel(GetString(Resource.String.Results), this.textSize, left, bottom+step, width, height, Paint.Align.Center, Color.Argb(255, 0, 170, 0));
				this.start = new CLLabel(GetString(Resource.String.Restart), this.textSize, left+step*3, bottom+step, width, height, Paint.Align.Center, Color.Argb(255, 0, 170, 0));
				this.exit = new CLLabel(GetString(Resource.String.Exit), this.textSize, left+step*6, bottom+step, width, height, Paint.Align.Center, Color.Argb(255, 0, 170, 0));
			} else {
				this.bestScore = new CLLabel(this.scoresTable[0,1], this.textSize, right-step*3, top, width, height, Paint.Align.Right, Color.Argb(255, 0, 170, 0));
				this.userScore = new CLLabel(this.score.ToString(), this.textSize, right-step*3, top-step, width, height, Paint.Align.Right, Color.Argb(255, 0, 170, 0));
				this.results = new CLLabel(GetString(Resource.String.Results), this.textSize, left, bottom+step, width, height, Paint.Align.Center, Color.Argb(255, 0, 170, 0));
				this.start = new CLLabel(GetString(Resource.String.Restart), this.textSize, left, bottom+step*2, width, height, Paint.Align.Center, Color.Argb(255, 0, 170, 0));
				this.exit = new CLLabel(GetString(Resource.String.Exit), this.textSize, right-step*3, bottom+step, width, height, Paint.Align.Center, Color.Argb(255, 0, 170, 0));
			}
			this.start.Action = delegate() {
				this.popUp = new CLLabel(GetString(Resource.String.RestartQ), this.textSize, -1.0f+step*2, 1.0f-step*4, step*5, height, Paint.Align.Center, Color.Argb(255, 255, 0, 0));
				this.popUp.ExtraDraw = delegate() {
					int textureId = (int)CLColour.CLMax+(int)CLLabelSize.CLMiddle;
					this.popUp.Draw(this.fieldTextures[textureId], 0.0f, 0.0f);
				};
				this.popUp.Action = delegate() {
					if (!CheckNewScore()) {
						this.ClearField();
						this.popUp = null;
					} else {
						RequestUserName();
					}
				};
				this.popUp.OutAction = delegate() {
					this.popUp = null;
				};
			};
			this.exit.Action = delegate() {
				this.popUp = new CLLabel(GetString(Resource.String.ExitQ), this.textSize, -1.0f+step*2, 1.0f-step*4, step*5, height, Paint.Align.Center, Color.Argb(255, 255, 0, 0));
				this.popUp.ExtraDraw = delegate() {
					int textureId = (int)CLColour.CLMax+(int)CLLabelSize.CLMiddle;
					this.popUp.Draw(this.fieldTextures[textureId], 0.0f, 0.0f);
				};
				this.popUp.Action = delegate() {
					this.quit = true;
					this.popUp = null;
				};
				this.popUp.OutAction = delegate() {
					this.popUp = null;
				};
			};
			this.results.Action = delegate() {
				this.popUp = new CLLabel(GetBestScoresList(), this.textSize*1.337f, -1.0f+step, 1.0f-step, step*7, step*7, Paint.Align.Center, Color.Argb(255, 0, 170, 0), 1.95f);
				this.popUp.ExtraDraw = delegate() {
					int textureId = (int)CLColour.CLMax+(int)CLLabelSize.CLLarge;
					CLReDraw.Rect(-1.0f+step, 1.0f-step, step*7, step*7, this.fieldTextures[textureId]);
				
					this.popUp.DrawText(0.0f, -step*3.5f);
				};
				this.popUp.Action = delegate() {
					this.popUp = null;
				};
				this.popUp.OutAction = delegate() {
					this.popUp = null;
				};
			};
		}
		public bool CheckLabelsActions(float x, float y) {
			if (this.popUp != null) {
				if (this.popUp.HitLabel(x, y)) {
					this.popUp.Action();
				} else {
					this.popUp.OutAction();
				}
				return true;
			} else if (this.exit.HitLabel(x, y)) {
				this.exit.Action();
				return true;
			} else if (this.start.HitLabel(x, y)) {
				this.start.Action();
				return true;
			} else if (this.results.HitLabel(x, y)) {
				this.results.Action();
				return true;
			}
			return false;
		}
		private string GetBestScoresList() {
			const int positionWidth = 19;
			string list = "";
			for (int i = 0; i < bestScoresCount; i++) {
				int dots = positionWidth - this.scoresTable[i,0].Length - this.scoresTable[i,1].Length - 4;
				list += String.Format("{0,2}", (i+1).ToString()) + ". " + this.scoresTable[i,0];
				for (int j = 0; j < dots; j++)
					list += ".";
				list += this.scoresTable[i,1] + "\n";
			}
			return list;
		}
		
		private void AddBalls(bool restart = false) {
			Random r = new Random(DateTime.Now.Millisecond);
			CLCell c = this.cells;
			int count = this.rows*this.columns - this.ballsCount;
			if (restart)
				count = 5;
			else if (count > 3)
				count = 3;
			for (int i = 0, n = count; i < n; i++, c = this.cells) {
				int column = r.Next(this.columns),
					row = r.Next(this.rows);
				for (int k = 0; k < row; k++)
					c = c.bottom;
				for (int l = 0; l < column; l++)
					c = c.right;
				//if the cell already has a ball then seek for the next empty one
				if (c.colour != CLColour.CLNone) {
					i--;
					continue;
				}
				if (!(restart && i > 2)) {
					c.colour = this.nextColours[i];
					this.nextColours[i] = (CLColour)r.Next((int)CLColour.CLRed, (int)CLColour.CLMax);
				} else {
					c.colour = (CLColour)r.Next((int)CLColour.CLRed, (int)CLColour.CLMax);
				}
				if (!restart && BlowBalls(c) && n < 3)
					n = 3;
			}
			this.ballsCount += count;
			int nextCount = this.rows*this.columns - this.ballsCount;
			if (nextCount == 1)
				this.nextColours[1] = this.nextColours[2] = CLColour.CLNone;
			else if (nextCount == 2)
				this.nextColours[2] = CLColour.CLNone;
			else if (nextCount <= 0)
				if (!CheckNewScore())
					this.ClearField();
				else {
					RequestUserName();
				}
		}
		protected enum CLDirection {
			CLLeft,
			CLRight,
			CLDown,
			CLUp,
			CLLeftDown,
			CLRightDown,
			CLLeftUp,
			CLRightUp,
		}
		private void BlowAnimation(CLCell newCell, CLCell cell, CLDirection dir) {
			CLCell c = cell;
			CLColour tempColour = newCell.colour;
			if (dir == CLDirection.CLLeft) {
				while (c.left != null && c.colour == c.left.colour) {
					c.colour = CLColour.CLNone;
					c = c.left;
					//TODO: add fancy opengl animations
				}
			} else if (dir == CLDirection.CLDown) {
				while (c.bottom != null && c.colour == c.bottom.colour) {
					c.colour = CLColour.CLNone;
					c = c.bottom;
					//TODO: add fancy opengl animations
				}
			} else if (dir == CLDirection.CLLeftDown) {
				while (c.bottom != null && c.left != null && c.colour == c.bottom.left.colour) {
					c.colour = CLColour.CLNone;
					c = c.bottom.left;
					//TODO: add fancy opengl animations
				}
			} else if (dir == CLDirection.CLRightDown) {
				while (c.bottom != null && c.right != null && c.colour == c.bottom.right.colour) {
					c.colour = CLColour.CLNone;
					c = c.bottom.right;
					//TODO: add fancy opengl animations
				}
			}
			c.colour = CLColour.CLNone;
			newCell.colour = tempColour;
		}
		private bool BlowBalls(CLCell cell) {
			bool willBlow = false;
			int count = 1, countTotal = 0;
			
			//just to be sure.....
			if (cell.colour == CLColour.CLNone)
				return false;

			/* LEFT-RIGHT (horizontal) balls sequence */
			CLCell c = cell;
			while (c.left != null && c.colour == c.left.colour) {
				count++;
				c = c.left;
			}
			c = cell;
			while (c.right != null && c.colour == c.right.colour) {
				count++;
				c = c.right;
			}
			if (count >= 5) {
				countTotal = count;
				willBlow = true;
				BlowAnimation(cell, c, CLDirection.CLLeft);
			}
			
			/* BOTTOM-TOP (vertical) balls sequence */
			count = 1;
			c = cell;
			while (c.bottom != null && c.colour == c.bottom.colour) {
				count++;
				c = c.bottom;
			}
			c = cell;
			while (c.top != null && c.colour == c.top.colour) {
				count++;
				c = c.top;
			}
			if (count >= 5) {
				if (willBlow)
					countTotal--;
				countTotal += count;
				willBlow = true;
				BlowAnimation(cell, c, CLDirection.CLDown);
			}
			
			/* BOTTOM|LEFT-TOP|RIGHT (increasing diagonal) balls sequence */
			count = 1;
			c = cell;
			while (c.bottom != null && c.left != null && c.colour == c.bottom.left.colour) {
				count++;
				c = c.bottom.left;
			}
			c = cell;
			while (c.top != null && c.right != null && c.colour == c.top.right.colour) {
				count++;
				c = c.top.right;
			}
			if (count >= 5) {
				if (willBlow)
					countTotal--;
				countTotal += count;
				willBlow = true;
				BlowAnimation(cell, c, CLDirection.CLLeftDown);
			}
			
			/* BOTTOM|RIGHT-TOP|LEFT (decreasing diagonal) balls sequence */
			count = 1;
			c = cell;
			while (c.bottom != null && c.right != null && c.colour == c.bottom.right.colour) {
				count++;
				c = c.bottom.right;
			}
			c = cell;
			while (c.top != null && c.left != null && c.colour == c.top.left.colour) {
				count++;
				c = c.top.left;
			}
			if (count >= 5) {
				if (willBlow)
					countTotal--;
				countTotal += count;
				willBlow = true;
				BlowAnimation(cell, c, CLDirection.CLRightDown);
			}

			if (willBlow) {
				this.ballsCount -= countTotal;
				this.score += AddScore(countTotal);
				this.userScore.text = this.score.ToString();
				cell.colour = CLColour.CLNone;
				//TODO: add fancy opengl animations
			}

			return willBlow;
		}
		public bool SelectBall(float x, float y) {
			CLCell c = GetCell(x, y);
			if (c == null)
				return false;
			if (c.colour == CLColour.CLNone)
				return false;
			if (this.selected == c) {
				this.selected.selected = false;
				this.selected = null;
				return true;
			}
			if (this.selected != null)
				this.selected.selected = false;
			this.selected = c;
			this.selected.selected = true;
			return true;
		}
		public bool IsReachable(CLCell from, CLCell to) {
			//the same point, wtf? should never happen
			if (from == to)
				return false;
			bool[,] map = new bool[this.columns, this.rows];
			CLCell cell = this.cells;
			for (int i = 0; i < this.rows; i++, cell = cell.bottom) {
				for (int j = 0; j < this.columns; j++, cell = cell.right) {
					if (cell == null)
						break;
					if (cell.colour == CLColour.CLNone)
						map[j, i] = true;
					else
						map[j, i] = false;
				}
				cell = this.cells;
				for (int k = 0; k < i; k++, cell = cell.bottom);
				if (cell == null)
					break;
			}
			SearchParameters searchParameters = new SearchParameters(new System.Drawing.Point(from.column, from.row), new System.Drawing.Point(to.column, to.row), map);
			PathFinder pathFinder = new PathFinder(searchParameters);
			if (pathFinder.HasPath())
				return true;
			return false;
		}
		public bool MoveBall(float x, float y) {
			if (this.selected == null)
				return false;
			CLCell from = this.selected,
				   to = GetCell(x, y);
			if (to == null)
				return false;
			if (!IsReachable(from, to))
				return false;
			to.colour = from.colour;
			from.colour = CLColour.CLNone;
			this.selected.selected = false;
			this.selected = null;
			if (!BlowBalls(to))
				AddBalls();
			return true;
		}
		private bool CheckNewScore() {
			int i = bestScoresCount - 1;
			for (;i >= 0 && int.Parse(this.scoresTable[i,1]) < this.score; i--);
			if (i > bestScoresCount-2)
				return false;
			return true;
		}
		private void SaveUserScore() {
			int i = bestScoresCount - 1;
			for (;i >= 0 && int.Parse(this.scoresTable[i,1]) < this.score; i--);
			for (int j = bestScoresCount-1; j > i+1; j--) {
				this.scoresTable[j,0] = this.scoresTable[j-1,0];
				this.scoresTable[j,1] = this.scoresTable[j-1,1];
			}
			this.scoresTable[i+1,0] = GLView.et.Text;
			this.scoresTable[i+1,1] = this.score.ToString();
			string scores = "";
			for (i = 0; i < bestScoresCount; i++)
				scores += this.scoresTable[i,0] + "\n" + this.scoresTable[i,1] + "\n";
			string appFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			string scoresFile = System.IO.Path.Combine(appFolder, "scores");
			using (FileStream fs = new FileStream(scoresFile, FileMode.OpenOrCreate, FileAccess.Write)) {
				using (MemoryStream ms = new MemoryStream(GetBytes(scores))) {
					ReadWriteStream(ms, fs);
				}
			}
		}
		private void RequestUserName() {
			float step;
			if (this.rows > this.columns)
				step = 2.0f / this.rows;
			else
				step = 2.0f / this.columns;
			this.popUp = new CLLabel(GetString(Resource.String.Name), this.textSize, -1.0f+step*2, 1.0f-step*4, step*5, step, Paint.Align.Left, Color.Argb(255, 0, 170, 0));
			this.popUp.Action = this.popUp.OutAction = delegate() {
				if (GLView.et.Text != "") {
					SaveUserScore();
					this.results.Action();
					this.popUp.Action = this.popUp.OutAction = delegate() {
						//TODO: add fancy adding new score animation
						ClearField();
						this.popUp = null;
					};
					((ViewGroup)GLView.et.Parent).RemoveView(GLView.et);
				}
			};
			this.popUp.ExtraDraw = delegate() {
				int textureId = (int)CLColour.CLMax+(int)CLLabelSize.CLMiddle;
				this.popUp.Draw(this.fieldTextures[textureId], 0.0f, 0.0f);
			};
			CLReDraw.EditText(-1.0f+step*3.45f, 1.0f-step*4.2f, this.textSize / 1.6f, Color.Argb(255, 0, 170, 0), this.popUp.Action);
		}
		static byte[] GetBytes(string str) {
			byte[] bytes = new byte[str.Length * sizeof(char)];
			System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
			return bytes;
		}
		static string GetString(byte[] bytes) {
			char[] chars = new char[bytes.Length / sizeof(char)];
			System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
			return new string(chars);
		}
		// readStream is the stream you need to read
		// writeStream is the stream you want to write to
		private void ReadWriteStream(Stream readStream, Stream writeStream) {
			int Length = 256;
			Byte[] buffer = new Byte[Length];
			int bytesRead = readStream.Read(buffer, 0, Length);
			// write the required bytes
			while (bytesRead > 0) {
				writeStream.Write(buffer, 0, bytesRead);
				bytesRead = readStream.Read(buffer, 0, Length);
			}
			readStream.Close();
			writeStream.Close();
		}

		public void ClearField(bool restart = true) {
			for (CLCell cb = this.cells; cb != null; cb = cb.bottom) {
				for (CLCell cr = cb; cr != null; cr = cr.right) {
					cr.colour = CLColour.CLNone;
				}
			}
			if (this.selected != null) {
				this.selected.selected = false;
				this.selected = null;
			}
			if (restart) {
				this.ballsCount = 0;
				Random r = new Random(DateTime.Now.Millisecond);
				for (uint i = 0; i < 3; i++) {
					this.nextColours[i] = (CLColour)r.Next((int)CLColour.CLRed, (int)CLColour.CLMax);
				}
				//TODO: add fancy restart animation
				AddBalls(true);
			}
			this.score = 0;
			this.userScore.text = this.score.ToString();
			this.bestScore.text = this.scoresTable[0,1];
		}

		private CLCell GetCell(float x, float y) {
			if (Math.Abs(x) > 1.0f || Math.Abs(y) > 1.0f)
				return null;
			int row = (int)((1.0f - y) / 2.0f * this.rows),
				column = (int)((x + 1.0f) / 2.0f * this.columns);
			return GetCell(row, column);
		}
		private CLCell GetCell(int row, int column) {
			CLCell cell = this.cells;
			for (uint i = 0; i < column; i++)
				cell = cell.right;
			for (uint j = 0; j < row; j++)
				cell = cell.bottom;
			return cell;
		}
	}
}