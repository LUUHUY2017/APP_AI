import QtQuick 2.15
import QtQuick.Controls 2.15
import QtQuick.Layouts 1.15
import QtGraphicalEffects 1.15

Rectangle {
    id: root
    width: 1100
    height: 960
    color: "#f5f5f5"

    // ==== Signal kết nối Python ====
    signal manualButtonPressed()
    signal manualButtonReleased()
    signal autoButtonClicked()
    signal abortButtonClicked()
    signal modeButtonClicked()
    signal sendButtonClicked(string text)
    signal settingsButtonClicked()
    signal titleMinimize()
    signal titleClose()
    signal titleDragStart(real mouseX, real mouseY)
    signal titleDragMoveTo(real mouseX, real mouseY)
    signal titleDragEnd()

    ColumnLayout {
        anchors.fill: parent
        spacing: 0

        // ==================== THANH TIÊU ĐỀ ====================
        Rectangle {
            id: titleBar
            Layout.fillWidth: true
            Layout.preferredHeight: 36
            color: "#f7f8fa"

            MouseArea {
                anchors.fill: parent
                acceptedButtons: Qt.LeftButton
                onPressed: root.titleDragStart(mouse.x, mouse.y)
                onPositionChanged: if (pressed) root.titleDragMoveTo(mouse.x, mouse.y)
                onReleased: root.titleDragEnd()
            }

            RowLayout {
                anchors.fill: parent
                anchors.leftMargin: 10
                anchors.rightMargin: 8
                spacing: 8
                Item { Layout.fillWidth: true }

                Rectangle {
                    width: 24; height: 24; radius: 6
                    color: btnMinMouse.pressed ? "#e5e6eb" : (btnMinMouse.containsMouse ? "#f2f3f5" : "transparent")
                    Text { anchors.centerIn: parent; text: "–"; font.pixelSize: 14; color: "#4e5969" }
                    MouseArea { id: btnMinMouse; anchors.fill: parent; hoverEnabled: true; onClicked: root.titleMinimize() }
                }

                Rectangle {
                    width: 24; height: 24; radius: 6
                    color: btnCloseMouse.pressed ? "#f53f3f" : (btnCloseMouse.containsMouse ? "#ff7875" : "transparent")
                    Text { anchors.centerIn: parent; text: "×"; font.pixelSize: 14; color: btnCloseMouse.containsMouse ? "white" : "#86909c" }
                    MouseArea { id: btnCloseMouse; anchors.fill: parent; hoverEnabled: true; onClicked: root.titleClose() }
                }
            }
        }

        // ==================== NỘI DUNG CHÍNH (status, emoji + tts side-by-side) ====================
        Rectangle {
            Layout.fillWidth: true
            Layout.fillHeight: true
            color: "transparent"

            ColumnLayout {
                anchors.fill: parent
                anchors.margins: 0
                spacing: 0

                // --- Thanh trạng thái (nhỏ) ---
                Rectangle {
                    id: statusBar
                    Layout.fillWidth: true
                    Layout.preferredHeight: 30
                    radius: 0
                    color: "#E3F2FD"
                    RowLayout {
                        anchors.fill: parent
                        anchors.margins: 0
                        anchors.leftMargin: 0
                        anchors.rightMargin: 8
                        spacing: 0
                        Item { Layout.fillWidth: true }
                        Text {
                            id: statusTextLabel
                            text: displayModel ? displayModel.statusText : "Trạng thái: Chưa kết nối"
                            font.pixelSize: 12
                            font.bold: false
                            color: "#1976D2"
                            horizontalAlignment: Text.AlignHCenter
                            verticalAlignment: Text.AlignVCenter
                            anchors.horizontalCenter: parent.horizontalCenter
                        }
                    }
                }

                // website link located visually at top-right (same vertical band as emoji target)
                RowLayout {
                    Layout.fillWidth: true
                    Layout.preferredHeight: 0
                    spacing: 0
                    Item { Layout.fillWidth: true }
					Text {
						id: websiteLink
						text: "Hỗ trợ"
						color: "#2196F3"
						font.pixelSize: 12
						anchors.right: parent.right
						anchors.rightMargin: 10  // 👈 tăng margin bên phải để không bị cắt chữ
					}
					MouseArea {
						anchors.fill: websiteLink
						hoverEnabled: true
						cursorShape: Qt.PointingHandCursor
						onClicked: Qt.openUrlExternally("https://heylily.net/lily")
						onEntered: websiteLink.color = "#1565C0"
						onExited: websiteLink.color = "#2196F3"
					}

                }

                // --- Hàng chính: emoji và khung TTS nằm cạnh nhau, căn chỉnh cao giống websiteLink ---
                RowLayout {
                    Layout.fillWidth: true
                    Layout.preferredHeight: 260
                    spacing: 20
                    anchors.leftMargin: 20
                    anchors.rightMargin: 20

                    // spacer to center the pair
                    Item { Layout.fillWidth: true }

                    // Emoji block
                    Item {
                        id: emojiBlock
                        Layout.preferredWidth: parent.width * 0.28
                        Layout.preferredHeight: parent.height * 0.6
                        width: Math.min(parent.width * 0.28, 260)
                        height: width
                        anchors.verticalCenter: parent.verticalCenter

                        Loader {
                            id: emotionLoader
                            anchors.centerIn: parent
                            property real sizeValue: Math.min(220, Math.max(80, parent.width * 0.6))
                            width: sizeValue
                            height: sizeValue
                            sourceComponent: {
                                var path = displayModel ? displayModel.emotionPath : ""
                                if (!path || path.length === 0) return emojiComponent
                                if (path.indexOf(".gif") !== -1) return gifComponent
                                if (path.indexOf(".") !== -1) return imageComponent
                                return emojiComponent
                            }
                            Component { id: gifComponent; AnimatedImage { source: displayModel.emotionPath; playing: true; fillMode: Image.PreserveAspectFit } }
                            Component { id: imageComponent; Image { source: displayModel.emotionPath; fillMode: Image.PreserveAspectFit } }
                            Component { id: emojiComponent; Text { id: emojiText; text: displayModel && displayModel.emotionPath ? displayModel.emotionPath : "🙂"; font.pixelSize: Math.min(180, sizeValue); horizontalAlignment: Text.AlignHCenter; verticalAlignment: Text.AlignVCenter } }
                        }
                    }

                    // TTS bubble next to emoji (same vertical center)
                    Rectangle {
                        id: ttsBubble
                        Layout.preferredWidth: parent.width * 0.45
                        Layout.preferredHeight: 120
                        width: Math.min(parent.width * 0.45, 520)
                        height: 120
                        radius: 18
                        color: "#ffffff"
                        border.color: "#d9d9d9"
                        border.width: 1
                        anchors.verticalCenter: parent.verticalCenter
                        layer.enabled: true
                        layer.effect: DropShadow {
                            color: "#22000000"
                            radius: 10
                            samples: 15
                            horizontalOffset: 0
                            verticalOffset: 2
                        }

                        // zoom effect when bot is speaking
                        SequentialAnimation {
                            id: zoomEffect
                            running: false
                            loops: Animation.Infinite
                            NumberAnimation { target: ttsBubble; property: "scale"; from: 1.0; to: 1.03; duration: 500; easing.type: Easing.InOutQuad }
                            NumberAnimation { target: ttsBubble; property: "scale"; from: 1.03; to: 1.0; duration: 500; easing.type: Easing.InOutQuad }
                        }

                        // optional small emoji pulse while speaking
                        SequentialAnimation {
                            id: emojiPulse
                            running: false
                            loops: Animation.Infinite
                            NumberAnimation { target: emotionLoader; property: "scale"; from: 1.0; to: 1.06; duration: 500; easing.type: Easing.InOutQuad }
                            NumberAnimation { target: emotionLoader; property: "scale"; from: 1.06; to: 1.0; duration: 500; easing.type: Easing.InOutQuad }
                        }

                        Connections {
                            target: displayModel
                            function onStatusTextChanged() {
                                var txt = displayModel.statusText || ""
                                if (txt.indexOf("Đang nói") !== -1 || txt.indexOf("đang nói") !== -1) {
                                    if (!zoomEffect.running) zoomEffect.start()
                                    if (!emojiPulse.running) emojiPulse.start()
                                } else {
                                    if (zoomEffect.running) zoomEffect.stop()
                                    if (emojiPulse.running) emojiPulse.stop()
                                    ttsBubble.scale = 1.0
                                    emotionLoader.scale = 1.0
                                }
                            }
                        }

                        Text {
                            id: ttsTextLabel
                            anchors.fill: parent
                            anchors.margins: 18
                            text: displayModel ? displayModel.ttsText : "Đang chờ..."
                            font.pixelSize: 14
                            color: "#333"
                            horizontalAlignment: Text.AlignHCenter
                            verticalAlignment: Text.AlignVCenter
                            wrapMode: Text.WordWrap
                        }
                    }

                    // spacer to center
                    Item { Layout.fillWidth: true }
                }
            }
        }

        // ==================== HÀNG DƯỚI (button + input) - gọn lại ====================
        Rectangle {
            Layout.fillWidth: true
            Layout.preferredHeight: 75
            color: "#f7f8fa"

            RowLayout {
                anchors.fill: parent
                anchors.leftMargin: 28
                anchors.rightMargin: 28
                anchors.bottomMargin: 12
                spacing: 12

                // --- Nút BẬT/TẮT với logic 'Đang tắt...' phục hồi ---
                Button {
                    id: toggleBtn
                    Layout.preferredWidth: 140
                    Layout.preferredHeight: 48
                    text: "BẬT"
                    property bool isOn: false
                    property bool pendingStop: false

                    background: Rectangle {
                        radius: 12
                        color: toggleBtn.pendingStop ? "#ff7875"
                            : (toggleBtn.isOn ? "#ff4d4f" : (toggleBtn.pressed ? "#0e42d2" : "#165dff"))
                    }

                    contentItem: Text {
                        text: toggleBtn.text
                        anchors.centerIn: parent
                        font.pixelSize: 16
                        color: "white"
                        horizontalAlignment: Text.AlignHCenter
                        verticalAlignment: Text.AlignVCenter
                    }

                    Connections {
                        target: displayModel
                        function onStatusTextChanged() {
                            var text = displayModel.statusText || ""
                            if (toggleBtn.pendingStop) {
                                if (text.indexOf("Đang lắng nghe") !== -1 || text.indexOf("đang lắng nghe") !== -1) {
                                    root.abortButtonClicked()
                                    root.modeButtonClicked()
                                    toggleBtn.pendingStop = false
                                    toggleBtn.isOn = false
                                    toggleBtn.text = "BẬT"
                                }
                            }
                        }
                    }

                    onClicked: {
                        if (!toggleBtn.isOn) {
                            root.autoButtonClicked()
                            toggleBtn.isOn = true
                            toggleBtn.text = "TẮT"
                        } else {
                            var curs = displayModel ? displayModel.statusText : ""
                            if (curs.indexOf("Đang nói") !== -1 || curs.indexOf("đang nói") !== -1) {
                                toggleBtn.pendingStop = true
                                toggleBtn.text = "Đang tắt..."
                            } else {
                                root.abortButtonClicked()
                                root.modeButtonClicked()
                                toggleBtn.isOn = false
                                toggleBtn.text = "BẬT"
                            }
                        }
                    }
                }

                // Input + buttons (compact)
                RowLayout {
                    Layout.fillWidth: true
                    Layout.preferredHeight: 48
                    spacing: 8

                    Rectangle {
                        Layout.fillWidth: true
                        Layout.preferredHeight: 48
                        color: "white"
                        radius: 12
                        border.color: textInput.activeFocus ? "#165dff" : "#e5e6eb"
                        border.width: 1

                        TextInput {
                            id: textInput
                            anchors.fill: parent
                            anchors.leftMargin: 12
                            anchors.rightMargin: 12
                            verticalAlignment: TextInput.AlignVCenter
                            font.pixelSize: 15
                            color: "#333"
                            selectByMouse: true

                            Text {
                                anchors.fill: parent
                                text: "Nhập văn bản..."
                                font: textInput.font
                                color: "#c9cdd4"
                                verticalAlignment: Text.AlignVCenter
                                visible: !textInput.text && !textInput.activeFocus
                            }

                            Keys.onReturnPressed: {
                                if (textInput.text.trim().length > 0) {
                                    root.sendButtonClicked(textInput.text)
                                    textInput.text = ""
                                }
                            }
                        }
                    }

                    Button {
                        id: sendBtn
                        Layout.preferredWidth: 90
                        Layout.preferredHeight: 48
                        text: "Gửi"
                        background: Rectangle {
                            radius: 12
                            color: sendBtn.pressed ? "#0e42d2" : "#165dff"
                        }
                        contentItem: Text {
                            text: sendBtn.text
                            anchors.centerIn: parent
                            font.pixelSize: 15
                            color: "white"
                            horizontalAlignment: Text.AlignHCenter
                            verticalAlignment: Text.AlignVCenter
                        }
                        onClicked: {
                            if (textInput.text.trim().length > 0) {
                                root.sendButtonClicked(textInput.text)
                                textInput.text = ""
                            }
                        }
                    }

                    Button {
                        id: settingsBtn
                        Layout.preferredWidth: 110
                        Layout.preferredHeight: 48
                        text: "Cấu hình"
                        background: Rectangle {
                            radius: 12
                            color: settingsBtn.pressed ? "#0e42d2" : "#165dff"
                        }
                        contentItem: Text {
                            text: settingsBtn.text
                            anchors.centerIn: parent
                            font.pixelSize: 15
                            color: "white"
                            horizontalAlignment: Text.AlignHCenter
                            verticalAlignment: Text.AlignVCenter
                        }
                        onClicked: root.settingsButtonClicked()
                    }
					
                }
            }
        }
    }
}
