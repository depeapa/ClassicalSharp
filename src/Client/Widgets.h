#ifndef CC_WIDGETS_H
#define CC_WIDGETS_H
#include "Gui.h"
#include "BlockID.h"
#include "Constants.h"
#include "Entity.h"
/* Contains all 2D widget implementations.
   Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
*/

void Widget_SetLocation(Widget* widget, UInt8 horAnchor, UInt8 verAnchor, Int32 xOffset, Int32 yOffset);

typedef struct TextWidget_ {
	Widget_Layout
	Texture Texture;
	Int32 DefaultHeight;
	FontDesc Font;

	bool ReducePadding;
	PackedCol Col;
} TextWidget;

void TextWidget_Make(TextWidget* widget, FontDesc* font);
void TextWidget_Create(TextWidget* widget, STRING_PURE String* text, FontDesc* font);
void TextWidget_SetText(TextWidget* widget, STRING_PURE String* text);


typedef void (*ButtonWidget_Set)(STRING_TRANSIENT String* raw);
typedef void (*ButtonWidget_Get)(STRING_TRANSIENT String* raw);

typedef struct ButtonWidget_ {
	Widget_Layout
	Texture Texture;
	Int32 DefaultHeight;
	FontDesc Font;

	String OptName;
	ButtonWidget_Get GetValue;
	ButtonWidget_Set SetValue;
	Int32 MinWidth, MinHeight;
} ButtonWidget;

void ButtonWidget_Create(ButtonWidget* widget, STRING_PURE String* text, Int32 minWidth, FontDesc* font, Widget_LeftClick onClick);
void ButtonWidget_SetText(ButtonWidget* widget, STRING_PURE String* text);


typedef struct ScrollbarWidget_ {
	Widget_Layout
	Int32 TotalRows, ScrollY;
	Real32 ScrollingAcc;
	Int32 MouseOffset;
	bool DraggingMouse;
} ScrollbarWidget;

void ScrollbarWidget_Create(ScrollbarWidget* widget);
void ScrollbarWidget_ClampScrollY(ScrollbarWidget* widget);


typedef struct HotbarWidget_ {
	Widget_Layout
	Texture SelTex, BackTex;
	Real32 BarHeight, SelBlockSize, ElemSize;
	Real32 BarXOffset, BorderSize;
	Real32 ScrollAcc;
	bool AltHandled;
} HotbarWidget;

void HotbarWidget_Create(HotbarWidget* widget);


typedef struct TableWidget_ {
	Widget_Layout
	Int32 ElementsCount, ElementsPerRow, RowsCount;
	Int32 LastCreatedIndex;
	FontDesc Font;
	Int32 SelectedIndex, BlockSize;
	Real32 SelBlockExpand;
	GfxResourceID VB;
	bool PendingClose;

	BlockID Elements[BLOCK_COUNT];
	ScrollbarWidget Scroll;
	Texture DescTex;
	Int32 LastX, LastY;
} TableWidget;

void TableWidget_Create(TableWidget* widget);
void TableWidget_SetBlockTo(TableWidget* widget, BlockID block);
void TableWidget_OnInventoryChanged(TableWidget* widget);
void TableWidget_MakeDescTex(TableWidget* widget, BlockID block);


#define INPUTWIDGET_MAX_LINES 3
#define INPUTWIDGET_LEN STRING_SIZE
typedef struct InputWidget_ {
	Widget_Layout
	FontDesc Font;		
	Int32 (*GetMaxLines)(void);
	void (*RemakeTexture)(GuiElement* elem);  /* Remakes the raw texture containing all the chat lines. Also updates dimensions. */
	void (*OnPressedEnter)(GuiElement* elem); /* Invoked when the user presses enter. */
	bool (*AllowedChar)(GuiElement* elem, UInt8 c);

	String Text;
	String Lines[INPUTWIDGET_MAX_LINES];     /* raw text of each line */
	Size2D LineSizes[INPUTWIDGET_MAX_LINES]; /* size of each line in pixels */
	Texture InputTex;
	String Prefix;
	UInt16 PrefixWidth, PrefixHeight;
	
	UInt8 Padding;
	bool ShowCaret;
	UInt16 CaretWidth;
	Int32 CaretX, CaretY;          /* Coordinates of caret in lines */
	Int32 CaretPos;                /* Position of caret, -1 for at end of string. */
	PackedCol CaretCol;
	Texture CaretTex;
	Real64 CaretAccumulator;
} InputWidget;

void InputWidget_Create(InputWidget* widget, FontDesc* font, STRING_REF String* prefix);
/* Calculates the sizes of each line in the text buffer. */
void InputWidget_CalculateLineSizes(InputWidget* widget);
/* Calculates the location and size of the caret character. */
void InputWidget_UpdateCaret(InputWidget* widget);
/* Clears all the characters from the text buffer. Deletes the native texture. */
void InputWidget_Clear(InputWidget* widget);
/* Appends a sequence of characters to current text buffer. May recreate the native texture. */
void InputWidget_AppendString(InputWidget* widget, STRING_PURE String* text);
/* Appends a single character to current text buffer. May recreate the native texture. */
void InputWidget_Append(InputWidget* widget, UInt8 c);


typedef struct MenuInputValidator_ {
	void (*GetRange)(struct MenuInputValidator_* validator, STRING_TRANSIENT String* range);
	bool (*IsValidChar)(struct MenuInputValidator_* validator, UInt8 c);
	bool (*IsValidString)(struct MenuInputValidator_* validator, STRING_PURE String* s);
	bool (*IsValidValue)(struct MenuInputValidator_* validator, STRING_PURE String* s);

	union {
		void* Meta_Ptr[2];
		Int32 Meta_Int[2];
		Real32 Meta_Real[2];
	};
} MenuInputValidator;

MenuInputValidator MenuInputValidator_Hex(void);
MenuInputValidator MenuInputValidator_Integer(Int32 min, Int32 max);
MenuInputValidator MenuInputValidator_Seed(void);
MenuInputValidator MenuInputValidator_Real(Real32 min, Real32 max);
MenuInputValidator MenuInputValidator_Path(void);
MenuInputValidator MenuInputValidator_Boolean(void);
MenuInputValidator MenuInputValidator_Enum(const UInt8** names, UInt32 namesCount);
MenuInputValidator MenuInputValidator_String(void);

typedef struct MenuInputWidget_ {
	InputWidget Base;
	Int32 MinWidth, MinHeight;
	MenuInputValidator Validator;
	UInt8 TextBuffer[String_BufferSize(INPUTWIDGET_LEN)];
} MenuInputWidget;

void MenuInputWidget_Create(MenuInputWidget* widget, Int32 width, Int32 height, STRING_PURE String* text, FontDesc* font, MenuInputValidator* validator);


typedef struct ChatInputWidget_ {
	InputWidget Base;
	Int32 TypingLogPos;
	UInt8 TextBuffer[String_BufferSize(INPUTWIDGET_MAX_LINES * INPUTWIDGET_LEN)];
	UInt8 OrigBuffer[String_BufferSize(INPUTWIDGET_MAX_LINES * INPUTWIDGET_LEN)];
} ChatInputWidget;

void ChatInputWidget_Create(ChatInputWidget* widget, FontDesc* font);


#define TEXTGROUPWIDGET_MAX_LINES 30
#define TEXTGROUPWIDGET_LEN (STRING_SIZE * 2)
typedef struct TextGroupWidget_ {
	Widget_Layout
	Int32 LinesCount, DefaultHeight;
	FontDesc Font, UnderlineFont;
	bool PlaceholderHeight[TEXTGROUPWIDGET_MAX_LINES];
	UInt8 LineLengths[TEXTGROUPWIDGET_MAX_LINES];
	Texture Textures[TEXTGROUPWIDGET_MAX_LINES];
	UInt8 Buffer[String_BufferSize(TEXTGROUPWIDGET_MAX_LINES * TEXTGROUPWIDGET_LEN)];
} TextGroupWidget;

void TextGroupWidget_Create(TextGroupWidget* widget, Int32 linesCount, FontDesc* font, FontDesc* underlineFont);
void TextGroupWidget_SetUsePlaceHolder(TextGroupWidget* widget, Int32 index, bool placeHolder);
void TextGroupWidget_PushUpAndReplaceLast(TextGroupWidget* widget, STRING_PURE String* text);
Int32 TextGroupWidget_UsedHeight(TextGroupWidget* widget);
void TextGroupWidget_GetSelected(TextGroupWidget* widget, STRING_TRANSIENT String* text, Int32 mouseX, Int32 mouseY);
void TextGroupWidget_GetText(TextGroupWidget* widget, Int32 index, STRING_TRANSIENT String* text);
void TextGroupWidget_SetText(TextGroupWidget* widget, Int32 index, STRING_PURE String* text);


typedef struct PlayerListWidget_ {
	Widget_Layout
	FontDesc Font;
	UInt16 NamesCount, ElementOffset;
	Int32 XMin, XMax, YHeight;
	bool Classic;
	TextWidget Overview;
	UInt16 IDs[TABLIST_MAX_NAMES * 2];
	Texture Textures[TABLIST_MAX_NAMES * 2];
} PlayerListWidget;

void PlayerListWidget_Create(PlayerListWidget* widget, FontDesc* font, bool classic);
void PlayerListWidget_GetNameUnder(PlayerListWidget* widget, Int32 mouseX, Int32 mouseY, STRING_TRANSIENT String* name);


typedef void (*SpecialInputAppendFunc)(void* userData, UInt8 c);
typedef struct SpecialInputTab_ {
	Int32 ItemsPerRow, CharsPerItem;
	Size2D TitleSize;
	String Title, Contents;	
} SpecialInputTab;
void SpecialInputTab_Init(SpecialInputTab* tab, STRING_REF String* title, 
	Int32 itemsPerRow, Int32 charsPerItem, STRING_REF String* contents);

typedef struct SpecialInputWidget_ {
	Widget_Layout
	Size2D ElementSize;
	Int32 SelectedIndex;
	InputWidget* AppendObj;
	Texture Tex;
	FontDesc Font;
	SpecialInputTab Tabs[5];
	String ColString;
	UInt8 ColBuffer[String_BufferSize(DRAWER2D_MAX_COLS * 4)];
} SpecialInputWidget;

void SpecialInputWidget_Create(SpecialInputWidget* widget, FontDesc* font, InputWidget* appendObj);
void SpecialInputWidget_UpdateCols(SpecialInputWidget* widget);
void SpecialInputWidget_SetActive(SpecialInputWidget* widget, bool active);
#endif